using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;
using static SkripsiAppBackend.UseCases.MetricCalculations;

namespace SkripsiAppBackend.Calculations
{
    public class TeamEvmCalculations
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;
        private readonly CommonCalculations common;

        public TeamEvmCalculations(
            Database database,
            IAzureDevopsService azureDevops,
            CommonCalculations common)
        {
            this.database = database;
            this.azureDevops = azureDevops;
            this.common = common;
        }

        public async Task<double> CalculateSchedulePerformanceIndex(string organizationName, string projectId, string teamId, DateTime now)
        {
            var actualEarnedValueTask = CalculateActualEarnedValue(organizationName, projectId, teamId, now);
            var plannedValueTask = CalculatePlannedValue(organizationName, projectId, teamId, now);

            await Task.WhenAll(actualEarnedValueTask, plannedValueTask);

            var schedulePerformanceIndex = Convert.ToDouble(actualEarnedValueTask.Result) / Convert.ToDouble(plannedValueTask.Result);

            return schedulePerformanceIndex;
        }
        
        public async Task<double> CalculateCostPerformanceIndex(string organizationName, string projectId, string teamId, DateTime now)
        {
            var reportedEarnedValueTask = CalculateReportedEarnedValue(organizationName, projectId, teamId, now);
            var actualCostTask = CalculateActualCost(organizationName, projectId, teamId, now);

            await Task.WhenAll(reportedEarnedValueTask, actualCostTask);

            var costPerformanceIndex = Convert.ToDouble(reportedEarnedValueTask.Result) / Convert.ToDouble(actualCostTask.Result);

            return costPerformanceIndex;
        }

        public async Task<long> CalculateActualCost(string organizationName, string projectId, string teamId, DateTime now)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var reports = await database.Reports.ReadTeamReports(teamKey);
            var actualCost = reports
                .Where(report => report.EndDate < now)
                .Aggregate(0L, (total, report) => total + report.Expenditure);

            return actualCost;
        }

        public async Task<long> CalculatePlannedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var workingDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);

            await Task.WhenAll(team, budgetAtCompletion, startDate, workingDays);

            if (!team.Result.Deadline.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            var deadline = (DateTime)team.Result.Deadline;

            var projectDuration = startDate.Result.WorkingDaysUntil(deadline, workingDays.Result);
            var actualDuration = startDate.Result.WorkingDaysUntil(now, workingDays.Result);

            var plannedValue = (Convert.ToInt64(actualDuration) * budgetAtCompletion.Result) / Convert.ToInt64(projectDuration);

            return plannedValue;
        }

        public async Task<long> CalculateActualEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var sprintsTask = common.GetAllSprintWorkItemsAsync(organizationName, projectId, teamId);

            await Task.WhenAll(team, sprintsTask);

            var sprintWorkItems = sprintsTask.Result
                .Where(sprint => sprint.Sprint.StartDate.HasValue)
                .Where(sprint => sprint.Sprint.StartDate < now)
                .SelectMany(sprint => sprint.WorkItems);
            var completedWorkItems = sprintWorkItems.Where(workItem => workItem.State == WorkItemState.Done);
            var completedEffort = common.CalculateTotalEffort(completedWorkItems.ToList());

            if (!team.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var actualEarnedValue = Convert.ToInt64(completedEffort) * (long)team.Result.CostPerEffort;

            return actualEarnedValue;
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var teamTask = database.TrackedTeams.ReadByKey(teamKey);
            var reportsTask = database.Reports.ReadTeamReports(teamKey);
            var sprintsTask = azureDevops.ReadTeamSprints(organizationName, projectId, teamId);

            await Task.WhenAll(teamTask, reportsTask, sprintsTask);

            var reportEffortTasks = reportsTask.Result
                .Where(report => report.EndDate < now)
                .Select(async (report) =>
                {
                    var reportSprints = sprintsTask.Result
                        .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                        .Where(sprint =>
                            sprint.StartDate >= report.StartDate && sprint.EndDate <= report.EndDate ||
                            sprint.StartDate <= report.StartDate && sprint.EndDate >= report.StartDate ||
                            sprint.StartDate <= report.EndDate && sprint.EndDate >= report.EndDate
                        );

                    var adjustedSprintTasks = reportSprints.Select(async (sprint) =>
                    {
                        var workItems = await azureDevops.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);

                        return common.AdjustSprint(sprint, workItems, report.StartDate, report.EndDate);
                    });

                    var adjustedSprints = (await Task.WhenAll(adjustedSprintTasks)).ToList();

                    var totalEffort = adjustedSprints.Aggregate(0d, (total, sprint) => total + sprint.Effort);

                    return totalEffort;
                });

            var reportEfforts = await Task.WhenAll(reportEffortTasks);

            var totalEffort = reportEfforts.Aggregate(0d, (total, effort) => total + effort);

            if (!teamTask.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var reportedEarnedValue = Convert.ToInt64(totalEffort) * (int)(teamTask.Result.CostPerEffort);

            return reportedEarnedValue;
        }

        public async Task<long> CalculateBudgetAtCompletion(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);

            await Task.WhenAll(team, totalEffort);

            var budgetAtCompletion = team.Result.CostPerEffort * totalEffort.Result;

            return Convert.ToInt64(budgetAtCompletion);
        }
    }
}
