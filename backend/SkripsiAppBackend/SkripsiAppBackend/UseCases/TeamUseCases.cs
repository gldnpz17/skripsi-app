using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
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

        public async Task<double> CalculateTeamEffort(string organizationName, string projectId, string teamId)
        {
            var backlogWorkItems = await azureDevopsService.ReadBacklogWorkItems(organizationName, projectId, teamId);
            var teamSprints = await GetSprintWorkItemsAsync(organizationName, projectId, teamId);
            var completedWorkItems = teamSprints
                    .SelectMany(sprintWorkItems => sprintWorkItems.WorkItems)
                    .Where(workItem => workItem.State == WorkItemState.Done);
            var incompleteWorkItems = teamSprints
                .SelectMany(sprintWorkItem => sprintWorkItem.WorkItems)
                .Where(workItem => workItem.State != WorkItemState.Done);

            var totalEffort = CalculateTotalEffort(backlogWorkItems.Concat(incompleteWorkItems).Concat(completedWorkItems).ToList());

            return totalEffort;
        }

        private double CalculateFeatureScore(double estimatedFeatureCompletion)
        {
            return estimatedFeatureCompletion * 2 - 1;
        }

        private double CalculateWorkingDaysBeforeDeadline(DateTime deadline, List<DayOfWeek> workDays)
        {
            if (deadline == null)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            return DateTime.Now.WorkingDaysUntil(deadline, workDays);
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

                totalWorkingDays += ((DateTime)startDate).WorkingDaysUntil((DateTime)endDate, workDays);
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
