using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.LoggingService;
using SkripsiAppBackend.UseCases.Extensions;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;
using static SkripsiAppBackend.Services.LoggingService.LoggingService.CalculationLog;

namespace SkripsiAppBackend.Calculations
{
    public class TeamEvmCalculations
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;
        private readonly LoggingService logging;
        private readonly CommonCalculations common;

        public TeamEvmCalculations(
            Database database,
            IAzureDevopsService azureDevops,
            LoggingService logging,
            CommonCalculations common)
        {
            this.database = database;
            this.azureDevops = azureDevops;
            this.logging = logging;
            this.common = common;
        }

        public enum EstimateAtCompletionFormulas
        {
            Unknown,
            Typical,
            Atypical,
            Typical2
        }

        public static class FormulaHelpers
        {
            public static TEnum? FromString<TEnum>(string stringValue) where TEnum : Enum
            {
                var values = Enum.GetValues(typeof(TEnum))
                    .Cast<TEnum>()
                    .ToList();

                foreach (var value in values)
                {
                    if (value.ToString() == stringValue)
                    {
                        return value;
                    }
                }

                return default;
            }
        }

        public async Task<long> CalculateEstimateToCompletion(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now,
            EstimateAtCompletionFormulas eacFormula)
        {
            var log = logging.CreateCalculationLog("Estimate to Completion (ETC)");
            log.Argument(new Args(organizationName, projectId, teamId, now, eacFormula));

            var estimateAtCompletion = CalculateEstimateAtCompletion(organizationName, projectId, teamId, now, eacFormula);
            var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);

            await Task.WhenAll(estimateAtCompletion, actualCost);

            var estimateToCompletion = estimateAtCompletion.Result - actualCost.Result;

            log.Record($"EAC = {estimateAtCompletion.Result}");
            log.Record($"AC = {actualCost.Result}");
            log.Record($"ETC = {estimateToCompletion}");

            log.Finish();

            return estimateToCompletion;
        }

        public async Task<long> CalculateEstimateAtCompletion(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now,
            EstimateAtCompletionFormulas eacFormula)
        {
            var log = logging.CreateCalculationLog("Estimate at Completion (EAC)");
            log.Argument(new Args(organizationName, projectId, teamId, now, eacFormula));

            var eac = eacFormula switch
            {
                EstimateAtCompletionFormulas.Typical => await CalculateBasic(),
                EstimateAtCompletionFormulas.Atypical => await CalculateAtypical(),
                EstimateAtCompletionFormulas.Typical2 => await CalculateTypical2(), // Just here for backwards compatibility. TODO: remove.
                _ => throw new Exception("Unknown formula")
            };

            log.Record($"EAC = {eac}");
            log.Finish();

            return eac;

            async Task<long> CalculateBasic()
            {
                var costPerformanceIndex = CalculateCostPerformanceIndex(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);

                await Task.WhenAll(costPerformanceIndex, budgetAtCompletion);

                log.Record($"CPI = {costPerformanceIndex.Result}");
                log.Record($"BAC = {budgetAtCompletion.Result}");

                return Convert.ToInt64(Convert.ToDouble(budgetAtCompletion.Result) / costPerformanceIndex.Result);
            }

            async Task<long> CalculateAtypical()
            {
                var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
                var earnedValue = CalculateReportedEarnedValue(organizationName, projectId, teamId, now);

                await Task.WhenAll(actualCost, budgetAtCompletion, earnedValue);

                log.Record($"AC = {actualCost.Result}");
                log.Record($"BAC = {budgetAtCompletion.Result}");
                log.Record($"EV = {earnedValue.Result}");

                return actualCost.Result + budgetAtCompletion.Result - earnedValue.Result;
            }

            async Task<long> CalculateTypical2()
            {
                var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
                var earnedValue = CalculateReportedEarnedValue(organizationName, projectId, teamId, now);
                var costPerformanceIndex = CalculateCostPerformanceIndex(organizationName, projectId, teamId, now);

                await Task.WhenAll(actualCost, budgetAtCompletion, earnedValue, costPerformanceIndex);

                return actualCost.Result + Convert.ToInt64(Convert.ToDouble(budgetAtCompletion.Result - earnedValue.Result) / costPerformanceIndex.Result);
            }
        }

        public async Task<double> CalculateSchedulePerformanceIndex(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateSchedulePerformanceIndex(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<double> CalculateSchedulePerformanceIndex(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end)
        {
            var log = logging.CreateCalculationLog("Schedule Performance Index (SPI)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var actualEarnedValueTask = CalculateActualEarnedValue(organizationName, projectId, teamId, start, end);
            var plannedValueTask = CalculatePlannedValue(organizationName, projectId, teamId, start, end);

            await Task.WhenAll(actualEarnedValueTask, plannedValueTask);

            var schedulePerformanceIndex = Convert.ToDouble(actualEarnedValueTask.Result) / Convert.ToDouble(plannedValueTask.Result);

            log.Record($"AEV = {actualEarnedValueTask.Result}");
            log.Record($"PV = {plannedValueTask.Result}");
            log.Record($"SPI = {schedulePerformanceIndex}");

            log.Finish();

            return schedulePerformanceIndex;
        }

        public async Task<double> CalculateCostPerformanceIndex(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateCostPerformanceIndex(organizationName, projectId, teamId, DateTime.MinValue, now);
        }
        
        public async Task<double> CalculateCostPerformanceIndex(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end)
        {
            var log = logging.CreateCalculationLog("Cost Performance Index (CPI)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var reportedEarnedValueTask = CalculateReportedEarnedValue(organizationName, projectId, teamId, start, end);
            var actualCostTask = CalculateActualCost(organizationName, projectId, teamId, start, end);

            await Task.WhenAll(reportedEarnedValueTask, actualCostTask);

            if (actualCostTask.Result == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.ZERO_EXPENDITURE);
            }

            var costPerformanceIndex = Convert.ToDouble(reportedEarnedValueTask.Result) / Convert.ToDouble(actualCostTask.Result);

            log.Record($"REV = {reportedEarnedValueTask.Result}");
            log.Record($"AC = {actualCostTask.Result}");
            log.Record($"CPI = {costPerformanceIndex}");

            log.Finish();

            return costPerformanceIndex;
        }

        public async Task<long> CalculateActualCost(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateActualCost(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateActualCost(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var log = logging.CreateCalculationLog("Actual Cost (AC)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var reports = await database.Reports.ReadTeamReports(teamKey);
            var actualCost = reports
                .Where(report => report.StartDate >= start && report.EndDate <= end)
                .Aggregate(0L, (total, report) =>
                {
                    log.Record($"(Report {report.Id}) Report duration={report.StartDate}-{report.EndDate}");

                    return total + report.Expenditure;
                });

            log.Record($"AC = {actualCost}");

            log.Finish();

            return actualCost;
        }

        public async Task<long> CalculatePlannedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            var startDate = await common.GetTeamStartDate(organizationName, projectId, teamId);
            return await CalculatePlannedValue(organizationName, projectId, teamId, startDate, now);
        }

        public async Task<long> CalculatePlannedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var log = logging.CreateCalculationLog("Planned Value (PV)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

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
            var actualDuration = start.Clamp(startDate.Result, deadline).WorkingDaysUntil(end.Clamp(startDate.Result, deadline), workingDays.Result);

            log.Record($"Accounted start = {start.Clamp(startDate.Result, deadline)}");
            log.Record($"Accounted end = {end.Clamp(startDate.Result, deadline)}");
            log.Record($"Project start date = {startDate.Result}");
            log.Record($"Project deadline = {deadline}");

            var plannedValue = (Convert.ToInt64(actualDuration) * budgetAtCompletion.Result) / Convert.ToInt64(projectDuration);

            log.Record($"Actual duration = {actualDuration}");
            log.Record($"Project duration = {projectDuration}");
            log.Record($"BAC = {budgetAtCompletion.Result}");
            log.Record($"PV = {plannedValue}");

            log.Finish();

            return plannedValue;
        }

        public async Task<long> CalculateActualEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateActualEarnedValue(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateActualEarnedValue(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end)
        {
            var log = logging.CreateCalculationLog("Actual Earned Value (AEV)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var sprintsTask = common.GetAllSprintWorkItemsAsync(organizationName, projectId, teamId);

            await Task.WhenAll(team, sprintsTask);

            var completedEffort = sprintsTask.Result
                .Where(sprint => sprint.Sprint.StartDate.HasValue && sprint.Sprint.EndDate.HasValue)
                .Where(sprint => 
                    ((DateTime)sprint.Sprint.StartDate).IsBetween(start, end) ||
                    ((DateTime)sprint.Sprint.EndDate).IsBetween(start, end)
                )
                .Select(sprint =>
                {
                    var adjustedSprint = common.AdjustSprint(
                        sprint.Sprint,
                        sprint.WorkItems.Where(workItem => workItem.State == WorkItemState.Done).ToList(),
                        start,
                        end
                    );

                    log.Record($"({sprint.Sprint.Name}) Duration={sprint.Sprint.StartDate}-{sprint.Sprint.EndDate}; Work Factor={adjustedSprint.WorkFactor}; Accounted Effort={adjustedSprint.Effort}");

                    return adjustedSprint;
                })
                .Aggregate(0d, (totalEffort, sprint) => totalEffort + sprint.Effort);

            if (!team.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var actualEarnedValue = completedEffort * (double)team.Result.CostPerEffort;

            log.Record($"Total completed effort = {completedEffort}");
            log.Record($"CPE = {team.Result.CostPerEffort}");
            log.Record($"AEV = {actualEarnedValue}");

            log.Finish();

            return (long)actualEarnedValue;
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateReportedEarnedValue(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var log = logging.CreateCalculationLog("Reported Earned Value (REV)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var teamTask = database.TrackedTeams.ReadByKey(teamKey);
            var reportsTask = database.Reports.ReadTeamReports(teamKey);
            var sprintsTask = azureDevops.ReadTeamSprints(organizationName, projectId, teamId);

            await Task.WhenAll(teamTask, reportsTask, sprintsTask);

            var reportEffortTasks = reportsTask.Result
                .Where(report => report.StartDate >= start && report.EndDate <= end)
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

                        var adjustedSprint = common.AdjustSprint(sprint, workItems.Where(workItem => workItem.State == WorkItemState.Done).ToList(), report.StartDate, report.EndDate);

                        log.Record($"(Report {report.Id} - {sprint.Name}) Report duration={report.StartDate}-{report.EndDate}; Sprint duration={sprint.StartDate}-{sprint.EndDate}; Work factor={adjustedSprint.WorkFactor}; Effort={adjustedSprint.Effort}");

                        return adjustedSprint;
                    });

                    var adjustedSprints = (await Task.WhenAll(adjustedSprintTasks)).ToList();

                    var totalEffort = adjustedSprints.Aggregate(0d, (total, sprint) => total + sprint.Effort);

                    return totalEffort;
                });

            var reportEfforts = await Task.WhenAll(reportEffortTasks);

            var totalEffort = reportEfforts.Aggregate(0d, (total, effort) => total + effort);

            log.Record($"Total effort = {totalEffort}");

            if (!teamTask.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var reportedEarnedValue = totalEffort * (double)(teamTask.Result.CostPerEffort);

            log.Record($"CPE = {teamTask.Result.CostPerEffort}");
            log.Record($"REV = {reportedEarnedValue}");

            log.Finish();

            return (long)reportedEarnedValue;
        }

        public async Task<long> CalculateBudgetAtCompletion(string organizationName, string projectId, string teamId)
        {
            var log = logging.CreateCalculationLog("Budget at Completion (BAC)");
            log.Argument(new Args(organizationName, projectId, teamId));

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);

            await Task.WhenAll(team, totalEffort);

            var budgetAtCompletion = team.Result.CostPerEffort * totalEffort.Result;

            log.Record($"CPE = {team.Result.CostPerEffort}");
            log.Record($"Total effort = {totalEffort.Result}");
            log.Record($"BAC = {budgetAtCompletion}");

            log.Finish();

            return Convert.ToInt64(budgetAtCompletion);
        }
    }
}
