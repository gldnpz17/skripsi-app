using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.Calculations
{
    public class CommonCalculations
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;

        public CommonCalculations(Database database, IAzureDevopsService azureDevops)
        {
            this.database = database;
            this.azureDevops = azureDevops;
        }

        public struct TimespanAdjustedSprint
        {
            public IAzureDevopsService.Sprint Sprint { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public double Effort { get; set; }
            public double WorkFactor { get; set; }
        }

        public struct SprintWorkItems
        {
            public Sprint Sprint { get; set; }
            public List<WorkItem> WorkItems { get; set; }
        }

        public async Task<double> CalculateTeamTotalEffort(string organizationName, string projectId, string teamId)
        {
            var backlogWorkItems = await azureDevops.ReadBacklogWorkItems(organizationName, projectId, teamId);
            var teamSprints = await GetAllSprintWorkItemsAsync(organizationName, projectId, teamId);
            var completedWorkItems = teamSprints
                .SelectMany(sprintWorkItems => sprintWorkItems.WorkItems)
                .Where(workItem => workItem.State == WorkItemState.Done);
            var incompleteWorkItems = teamSprints
                .SelectMany(sprintWorkItem => sprintWorkItem.WorkItems)
                .Where(workItem => workItem.State != WorkItemState.Done);

            var totalEffort = CalculateTotalEffort(backlogWorkItems.Concat(incompleteWorkItems).Concat(completedWorkItems).ToList());

            return totalEffort;
        }

        public async Task<List<SprintWorkItems>> GetAllSprintWorkItemsAsync(string organizationName, string projectId, string teamId)
        {
            var sprints = await azureDevops.ReadTeamSprints(organizationName, projectId, teamId);

            var sprintWorkItems = new List<SprintWorkItems>();
            var fetchTasks = sprints.Select(async (sprint) =>
            {
                var workItems = await azureDevops.ReadSprintWorkItems(
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

        public double CalculateTotalEffort(List<WorkItem> workItems)
        {
            return CalculateTotalEffort(workItems, (workItem) => workItem.Effort);
        }

        public double CalculateTotalEffort(List<TimespanAdjustedSprint> sprints)
        {
            return CalculateTotalEffort(sprints, (sprint) => sprint.Effort);
        }

        public double CalculateTotalEffort<T>(List<T> items, Func<T, double> getEffort)
        {
            double totalEffort = 0;
            items.ForEach(item => totalEffort += getEffort(item));
            return totalEffort;
        }

        public async Task<DateTime> GetTeamStartDate(string organizationName, string projectId, string teamId)
        {
            var teamSprints = await azureDevops.ReadTeamSprints(organizationName, projectId, teamId);

            var earliestSprint = teamSprints
                .Where(sprint => sprint.StartDate.HasValue)
                .OrderBy(sprint => sprint.StartDate)
                .FirstOrDefault();

            if (!earliestSprint.StartDate.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            return (DateTime)earliestSprint.StartDate;
        }

        public async Task<DateTime> GetLatestReportDate(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var reports = await database.Reports.ReadTeamReports(teamKey);

            return reports.Max(report => report.EndDate);
        }

        public async Task<List<TimespanAdjustedSprint>> ReadAdjustedSprints(
            string organizationName, 
            string projectId,
            string teamId,
            DateTime startDate,
            DateTime endDate)
        {
            var sprints = (await azureDevops.ReadTeamSprints(organizationName, projectId, teamId))
                .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                .Where(sprint =>
                    (sprint.StartDate >= startDate && sprint.EndDate <= endDate) ||
                    (sprint.StartDate <= startDate && sprint.EndDate >= startDate) ||
                    (sprint.StartDate <= endDate && sprint.EndDate >= endDate)
                )
                .Select(async (sprint) =>
                {
                    var workItems = await azureDevops.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);

                    return AdjustSprint(sprint, workItems, startDate, endDate);
                });

            var adjustedSprints = await Task.WhenAll(sprints);

            return adjustedSprints.ToList();
        }

        public TimespanAdjustedSprint AdjustSprint(
            Sprint sprint,
            List<WorkItem> workItems,
            DateTime startDate,
            DateTime endDate)
        {
            DateTime sprintStartDate = (DateTime)sprint.StartDate;
            DateTime sprintEndDate = (DateTime)sprint.EndDate;

            var startTicks = Math.Max(sprintStartDate.Ticks, startDate.Ticks);
            var accountedStartDate = new DateTime(startTicks);
            var endTicks = Math.Min(sprintEndDate.Ticks, endDate.Ticks);
            var accountedEndDate = new DateTime(endTicks);

            var accountedDuration = (accountedEndDate - accountedStartDate).TotalDays;
            var sprintDuration = (sprintEndDate - sprintStartDate).TotalDays;
            var workFactor = accountedDuration / sprintDuration;

            var totalEffort = CalculateTotalEffort(workItems);

            return new TimespanAdjustedSprint()
            {
                Sprint = sprint,
                WorkFactor = workFactor,
                Effort = totalEffort * workFactor,
                StartDate = accountedStartDate,
                EndDate = accountedEndDate
            };
        }

        public DateTime MinDateTime(params DateTime[] dateTimes)
        {
            var min = dateTimes[0];
            foreach (var dateTime in dateTimes)
            {
                if (dateTime < min)
                {
                    min = dateTime;
                }
            }
            return min;
        }
    }
}
