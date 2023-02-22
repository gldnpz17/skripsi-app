using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using static SkripsiAppBackend.Common.Exceptions.UserFacingException;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.UseCases
{
    public class TeamUseCases
    {
        private readonly IAzureDevopsService azureDevopsService;
        private readonly Configuration configuration;
        private readonly Database database;

        public TeamUseCases(
            IAzureDevopsService azureDevopsService, 
            Configuration configuration, 
            Database database)
        {
            this.azureDevopsService = azureDevopsService;
            this.configuration = configuration;
            this.database = database;
        }

        public struct SprintWorkItems
        {
            public Sprint Sprint { get; set; }
            public List<WorkItem> WorkItems { get; set; }
        }

        public enum Severity
        {
            Healthy,
            AtRisk,
            Critical
        }

        public struct TimelinessMetric
        {
            public double? Score { get; set; }
            public string? Severity { get; set; }
            public DateTime? EstimatedCompletionDate { get; set; }
            public int? TargetDateErrorInDays { get; set; }
            public string? ErrorCode { get; set; }
        }

        public struct FeatureMetric
        {
            public double? Score { get; set; }
            public string? Severity { get; set; }
            public double? EstimatedFeatureCompletion { get; set; }
            public string? ErrorCode { get; set; }
        }

        public async Task<TimelinessMetric> CalculateTimelinessMetricAsync(string organizationName, string projectId, string teamId)
        {
            double? score = null;
            Severity? severity = null;
            DateTime? estimatedCompletionDate = null;
            int? targetDateErrorInDays = null;
            string? errorCode = null;

            try
            {
                var backlogWorkItems = await azureDevopsService.ReadBacklogWorkItems(organizationName, projectId, teamId);
                var teamSprints = await GetSprintWorkItemsAsync(organizationName, projectId, teamId);

                var completedWorkItems = teamSprints
                    .SelectMany(sprintWorkItems => sprintWorkItems.WorkItems)
                    .Where(workItem => workItem.State == WorkItemState.Done);
                var incompleteWorkItems = teamSprints
                    .SelectMany(sprintWorkItem => sprintWorkItem.WorkItems)
                    .Where(workItem => workItem.State != WorkItemState.Done);

                var teamWorkDays = await azureDevopsService.ReadTeamWorkDays(organizationName, projectId, teamId);
                var deadline = (await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId)).Deadline;
                var marginFactor = configuration.TimelinessMarginFactor;

                var startDate = CalculateStartDate(teamSprints);
                var velocity = CalculateVelocity(teamSprints, teamWorkDays, 3);
                var totalEffort = CalculateTotalEffort(backlogWorkItems.Concat(incompleteWorkItems).Concat(completedWorkItems).ToList());
                var estimatedTotalWorkingDays = CalculateEstimatedTotalWorkingDays(velocity, totalEffort);
                var targetTotalWorkingDays = WorkingDaysBetween((DateTime)startDate, (DateTime)deadline, teamWorkDays);

                var targetErrorWorkingDays = CalculateTargetErrorWorkingDays(estimatedTotalWorkingDays, targetTotalWorkingDays);
                estimatedCompletionDate = AddWorkingDays((DateTime)startDate, estimatedTotalWorkingDays, teamWorkDays);
                score = CalculateTimelinessScore(targetTotalWorkingDays, marginFactor, targetErrorWorkingDays);
                severity = CalculateTimelinessSeverity((double)score);
                targetDateErrorInDays = Convert.ToInt32(Math.Ceiling(targetErrorWorkingDays));
            }
            catch (UserFacingException exception)
            {
                errorCode = exception.ErrorCode.ToString();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                errorCode = UserFacingException.ErrorCodes.UNKNOWN_ERROR.ToString();
            }

            return new TimelinessMetric()
            {
                Score = score,
                Severity = severity.ToString(),
                EstimatedCompletionDate = estimatedCompletionDate,
                TargetDateErrorInDays = targetDateErrorInDays,
                ErrorCode = errorCode
            };
        }

        public async Task<FeatureMetric> CalculateFeatureMetricAsync(string organizationName, string projectId, string teamId)
        {
            double? estimatedFeatureCompletion = null;
            double? score = null;
            Severity? severity = null;
            string? errorCode = null;

            try
            {
                var teamSprints = await GetSprintWorkItemsAsync(organizationName, projectId, teamId);
                var backlogWorkItems = await azureDevopsService.ReadBacklogWorkItems(organizationName, projectId, teamId);
                var teamWorkDays = await azureDevopsService.ReadTeamWorkDays(organizationName, projectId, teamId);
                var deadline = (await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId)).Deadline;

                var remainingWorkItems = teamSprints
                    .SelectMany(sprintWorkItem => sprintWorkItem.WorkItems)
                    .Where(workItem => workItem.State != WorkItemState.Done)
                    .Concat(backlogWorkItems)
                    .ToList();
                var completedWorkItems = teamSprints
                    .SelectMany(sprintWorkItem => sprintWorkItem.WorkItems)
                    .Where(workItem => workItem.State == WorkItemState.Done)
                    .ToList();

                var velocity = CalculateVelocity(teamSprints, teamWorkDays, 3);
                var remainingWorkingDays = CalculateWorkingDaysBeforeDeadline((DateTime)deadline, teamWorkDays);

                var estimatedTotalBusinessValue = CalculateEstimatedTotalBusinessValue(
                    velocity,
                    remainingWorkingDays,
                    remainingWorkItems,
                    completedWorkItems);
                var targetTotalBusinessValue = CalculateTargetTotalBusinessValue(
                    remainingWorkItems
                        .Concat(completedWorkItems)
                        .ToList());

                estimatedFeatureCompletion = CalculateEstimatedFeatureCompletion(
                    estimatedTotalBusinessValue,
                    targetTotalBusinessValue);
                score = CalculateFeatureScore((double)estimatedFeatureCompletion);
                severity = CalculateFeatureSeverity((double)score);
            }
            catch (UserFacingException exception)
            {
                errorCode = exception.ErrorCode.ToString();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                errorCode = UserFacingException.ErrorCodes.UNKNOWN_ERROR.ToString();
            }

            return new FeatureMetric()
            {
                Score = estimatedFeatureCompletion,
                EstimatedFeatureCompletion = estimatedFeatureCompletion,
                Severity = severity.ToString(),
                ErrorCode = errorCode
            };
        }

        private double CalculateFeatureScore(double estimatedFeatureCompletion)
        {
            return estimatedFeatureCompletion * 2 - 1;
        }

        private Severity CalculateFeatureSeverity(double score)
        {
            if (score < -1 || score > 1)
            {
                throw new ArgumentException("Invalid score range.");
            }

            if (score < 0)
            {
                return Severity.Critical;
            }
            else if (score >= 0 && score <= 0.5)
            {
                return Severity.AtRisk;
            }
            else
            {
                return Severity.Healthy;
            }
        }

        private double CalculateWorkingDaysBeforeDeadline(DateTime deadline, List<DayOfWeek> workDays)
        {
            if (deadline == null)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            return WorkingDaysBetween(DateTime.Now, deadline, workDays);
        }

        private double CalculateEstimatedFeatureCompletion(double estimatedTotalBusinessValue, double targetTotalBusinessValue)
        {
            return estimatedTotalBusinessValue / targetTotalBusinessValue;
        }

        private double CalculateEstimatedTotalBusinessValue(
            double velocity,
            double remainingWorkingDays,
            List<WorkItem> remainingWorkItems,
            List<WorkItem> completedWorkItems)
        {
            var orderedWorkItems = remainingWorkItems.OrderBy(workItem => workItem.Priority);

            var effortCapacity = velocity * remainingWorkingDays;

            var completableWorkItems = new List<WorkItem>();

            foreach (var workItem in orderedWorkItems)
            {
                if (effortCapacity - workItem.Effort < 0)
                {
                    break;
                }

                completableWorkItems.Add(workItem);
            }

            double estimatedTotalBusinessValue = 0;

            foreach (var workItem in completedWorkItems.Concat(completableWorkItems))
            {
                estimatedTotalBusinessValue += workItem.BusinessValue;
            }

            return estimatedTotalBusinessValue;
        }

        private double CalculateTargetTotalBusinessValue(List<WorkItem> allWorkItems)
        {
            double totalBusinessValue = 0;
            
            foreach (var workItem in allWorkItems)
            {
                totalBusinessValue += workItem.BusinessValue;
            }

            return totalBusinessValue;
        }

        private DateTime CalculateStartDate(List<SprintWorkItems> sprintWorkItems)
        {
            var validSprints = sprintWorkItems.Where(sprint => sprint.Sprint.StartDate.HasValue);

            if (validSprints.Count() == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            var startDate = validSprints
                    .OrderBy(sprint => sprint.Sprint.StartDate)
                    .First()
                    .Sprint.StartDate;

            return (DateTime)startDate;
        }

        private double CalculateTimelinessScore(double targetTotalWorkingDays, double marginFactor, double targetErrorWorkingDays)
        {
            var marginDays = targetTotalWorkingDays * marginFactor;
            var rawScore = targetErrorWorkingDays / marginDays;

            return Math.Clamp(rawScore, -1, 1);
        }

        private Severity CalculateTimelinessSeverity(double score)
        {
            if (score < -1 || score > 1)
            {
                throw new ArgumentException("Invalid score range.");
            }

            if (score < 0)
            {
                return Severity.Critical;
            }
            else if (score >= 0 && score <= 0.1)
            {
                return Severity.AtRisk;
            }
            else
            {
                return Severity.Healthy;
            }
        }

        private double CalculateVelocity(List<SprintWorkItems> sprintWorkItems, List<DayOfWeek> workDays, int windowSize)
        {
            var windowSprintWorkItems = sprintWorkItems
                .Where(sprintWorkItem =>
                    sprintWorkItem.Sprint.TimeFrame == SprintTimeFrame.Current ||
                    sprintWorkItem.Sprint.TimeFrame == SprintTimeFrame.Past)
                .OrderBy(sprintWorkItem => sprintWorkItem.Sprint.EndDate)
                .Take(windowSize)
                .ToList();

            if (windowSprintWorkItems.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            var completedWorkItems = windowSprintWorkItems.SelectMany(sprintWorkItem => sprintWorkItem.WorkItems).ToList();

            var totalEffort = CalculateTotalEffort(completedWorkItems);
            double totalWorkingDays = 0;
            foreach (var sprintWorkItem in windowSprintWorkItems)
            {
                var startDate = sprintWorkItem.Sprint.StartDate;
                var endDate = sprintWorkItem.Sprint.EndDate;

                if (!startDate.HasValue || !endDate.HasValue)
                {
                    throw new UserFacingException(UserFacingException.ErrorCodes.SPRINT_INVALID_DATE);
                }

                totalWorkingDays += WorkingDaysBetween((DateTime)startDate, (DateTime)endDate, workDays);
            }

            return totalEffort / totalWorkingDays;
        }

        private async Task<List<SprintWorkItems>> GetSprintWorkItemsAsync(string organizationName, string projectId, string teamId)
        {
            var sprints = await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId);
            
            var sprintWorkItems = new List<SprintWorkItems>();
            var fetchTasks = sprints.Select(async (sprint) =>
            {
                var workItems = await azureDevopsService.ReadSprintWorkItems(
                    organizationName,
                    projectId,
                    teamId,
                    sprint.Id);

                sprintWorkItems.Add(new SprintWorkItems()
                {
                    Sprint = sprint,
                    WorkItems = workItems
                });
            });

            await Task.WhenAll(fetchTasks);

            return sprintWorkItems;
        }

        private double CalculateEstimatedTotalWorkingDays(double velocity, double totalEffort)
        {
            return totalEffort / velocity;
        }

        private double WorkingDaysBetween(DateTime startDate, DateTime endDate, List<DayOfWeek> workDays)
        {
            var currentDate = new DateTime(startDate.Ticks);
            double workingDays = 0;
            
            while (currentDate < endDate)
            {
                if (workDays.Contains(currentDate.DayOfWeek))
                {
                    workingDays += 1;
                }

                currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }

        private double CalculateTargetErrorWorkingDays(double targetTotalWorkingDays, double estimatedTotalWorkingDays)
        {
            return estimatedTotalWorkingDays - targetTotalWorkingDays;
        }

        private DateTime AddWorkingDays(DateTime startDate, double workingDays, List<DayOfWeek> workDays)
        {
            var endDate = new DateTime(startDate.Ticks);

            // If there's still a full day and today's not a holiday.
            while (workingDays >= 1 && !workDays.Contains(endDate.DayOfWeek))
            {
                endDate = endDate.AddDays(1);
                if (workDays.Contains(endDate.DayOfWeek))
                {
                    workingDays--;
                }
            }

            // Add the remainder of the day.
            endDate = endDate.AddDays(workingDays);

            return endDate;
        }

        public DateTime GetEstimatedEndDate(DateTime startDate, int remainingWorkingDays, List<DayOfWeek> workDays)
        {
            // TODO: Probably would need to accomodate for national holidays?
            var endDate = new DateTime(startDate.Ticks);
            while (remainingWorkingDays > 0)
            {
                endDate = endDate.AddDays(1);
                if (workDays.Contains(endDate.DayOfWeek))
                {
                    remainingWorkingDays--;
                }
            }

            return endDate;
        }

        public double CalculateTotalEffort(List<WorkItem> workItems)
        {
            double totalEffort = 0;
            workItems.ForEach(workItem => totalEffort += workItem.Effort);
            return totalEffort;
        }
    }
}
