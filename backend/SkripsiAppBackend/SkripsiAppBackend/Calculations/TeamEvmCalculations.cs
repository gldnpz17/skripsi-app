using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.LoggingService;
using SkripsiAppBackend.UseCases.Extensions;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeams.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;
using static SkripsiAppBackend.Services.LoggingService.LoggingService;
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
                EstimateAtCompletionFormulas.Typical => await CalculateTypical(),
                EstimateAtCompletionFormulas.Atypical => await CalculateAtypical(),
                _ => throw new Exception("Unknown formula")
            };

            log.Record($"EAC = {eac}");
            log.Finish();

            return eac;

            async Task<long> CalculateTypical()
            {
                var costPerformanceIndex = CalculateCostPerformanceIndex(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);

                await Task.WhenAll(costPerformanceIndex, budgetAtCompletion);

                log.Record($"CPI = {costPerformanceIndex.Result}");
                log.Record($"BAC = {budgetAtCompletion.Result}");

                return CostEstimationFormulas.Typical(costPerformanceIndex.Result, budgetAtCompletion.Result);
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

                return CostEstimationFormulas.Atypical(actualCost.Result, budgetAtCompletion.Result, earnedValue.Result);
            }
        }

        private static class CostEstimationFormulas
        {
            public static long Typical(double costPerformanceIndex, long budget)
            {
                return Convert.ToInt64(Convert.ToDouble(budget) / costPerformanceIndex);
            }

            public static long Atypical(long actualCost, long budget, long earnedValue)
            {
                return actualCost + budget - earnedValue;
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

        public async Task<double> CalculatePlannedDuration(
            string organizationName,
            string projectId,
            string teamId,
            CalculationLog? log = null)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var workDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);

            await Task.WhenAll(team, workDays, startDate);

            var plannedDuration = startDate.Result.WorkingDaysUntil((DateTime)team.Result.Deadline, workDays.Result);

            log?.Record($"Planned duration = {plannedDuration}");

            return plannedDuration;
        }

        public async Task<DateTime> CalculateEstimatedCompletionDate(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now)
        {
            var log = logging.CreateCalculationLog("Estimated Completion Date");
            log.Argument(new Args(organizationName, projectId, teamId, now));
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var schedulePerformanceIndex = CalculateSchedulePerformanceIndex(organizationName, projectId, teamId, now);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var team = database.TrackedTeams.ReadByKey(teamKey);
            var workDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);
            var plannedDuration = CalculatePlannedDuration(organizationName, projectId, teamId);

            await Task.WhenAll(schedulePerformanceIndex, startDate, team, workDays, plannedDuration);

            if (!team.Result.Deadline.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            log.Record($"Start date = {startDate.Result}");
            log.Record($"Deadline = {team.Result.Deadline}");
            log.Record($"SPI = {schedulePerformanceIndex.Result}");
            log.Record($"Work days = {workDays.Result}");

            var estimatedDuration = plannedDuration.Result / schedulePerformanceIndex.Result;
            log.Record($"Estimated duration = {estimatedDuration}");

            var estimatedCompletionDate = startDate.Result.AddWorkingDays(estimatedDuration, workDays.Result);
            log.Record($"Estimated completion date = {estimatedCompletionDate}");

            log.Finish();

            return estimatedCompletionDate;
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
                .Where(report => report.StartDate.IsBetween(start, end) || report.EndDate.IsBetween(start, end))
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

        private async Task<double> CalculateReportedEffort(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end,
            CalculationLog? log = null)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);
            var reportsTask = database.Reports.ReadTeamReports(teamKey);
            var sprintsTask = azureDevops.ReadTeamSprints(organizationName, projectId, teamId);

            await Task.WhenAll(reportsTask, sprintsTask);

            var reportEffortTasks = reportsTask.Result
                .Where(report => report.StartDate.IsBetween(start, end) || report.EndDate.IsBetween(start, end))
                .Select(async (report) =>
                {
                    var reportSprints = sprintsTask.Result
                        .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                        .Where(sprint =>
                            ((DateTime)sprint.StartDate).IsBetween(report.StartDate, report.EndDate) ||
                            ((DateTime)sprint.EndDate).IsBetween(report.StartDate, report.EndDate)
                        );

                    var adjustedSprintTasks = reportSprints.Select(async (sprint) =>
                    {
                        var workItems = await azureDevops.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);

                        var adjustedSprint = common.AdjustSprint(sprint, workItems.Where(workItem => workItem.State == WorkItemState.Done).ToList(), report.StartDate, report.EndDate);

                        log?.Record($"(Report {report.Id} - {sprint.Name}) Report duration={report.StartDate}-{report.EndDate}; Sprint duration={sprint.StartDate}-{sprint.EndDate}; Work factor={adjustedSprint.WorkFactor}; Effort={adjustedSprint.Effort}");

                        return adjustedSprint;
                    });

                    var adjustedSprints = (await Task.WhenAll(adjustedSprintTasks)).ToList();

                    var totalEffort = adjustedSprints.Aggregate(0d, (total, sprint) => total + sprint.Effort);

                    return totalEffort;
                });

            var reportEfforts = await Task.WhenAll(reportEffortTasks);

            var totalEffort = reportEfforts.Aggregate(0d, (total, effort) => total + effort);

            return totalEffort;
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var log = logging.CreateCalculationLog("Reported Earned Value (REV)");
            log.Argument(new Args(organizationName, projectId, teamId, start, end));

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var teamTask = database.TrackedTeams.ReadByKey(teamKey);
            var totalEffort = CalculateReportedEffort(organizationName, projectId, teamId, start, end, log);

            await Task.WhenAll(teamTask, totalEffort);

            log.Record($"Total effort = {totalEffort}");

            if (!teamTask.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var reportedEarnedValue = totalEffort.Result * (double)(teamTask.Result.CostPerEffort);

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

        public async Task<long> CalculateEstimatedEarnedValue(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now,
            DateTime estimationDate)
        {
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var estimatedCompletionDate = CalculateEstimatedCompletionDate(organizationName, projectId, teamId, common.MinDateTime(now, estimationDate));
            var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var workDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);

            await Task.WhenAll(startDate, estimatedCompletionDate, budgetAtCompletion, workDays);

            var totalDuration = startDate.Result.WorkingDaysUntil(estimatedCompletionDate.Result, workDays.Result);
            var estimationDuration = startDate.Result.WorkingDaysUntil(estimationDate.Clamp(startDate.Result, estimatedCompletionDate.Result), workDays.Result);

            var earnedValue = Convert.ToInt64((estimationDuration / totalDuration) * budgetAtCompletion.Result);

            return earnedValue;
        }

        public async Task<long> CalculateEstimatedActualCost(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now,
            DateTime estimationDate,
            EstimateAtCompletionFormulas eacFormula)
        {
            var latestReportDate = await common.GetLatestReportDate(organizationName, projectId, teamId);

            if (estimationDate <= latestReportDate)
            {
                return await CalculateActualCost(organizationName, projectId, teamId, estimationDate);
            }

            var eac = eacFormula switch
            {
                EstimateAtCompletionFormulas.Typical => await CalculateTypical(),
                EstimateAtCompletionFormulas.Atypical => await CalculateAtypical(),
                _ => throw new Exception("Unknown formula")
            };

            return eac;

            async Task<long> CalculateTypical()
            {
                var earnedValue = CalculateEstimatedEarnedValue(organizationName, projectId, teamId, now, estimationDate);
                var costPerformanceIndex = CalculateCostPerformanceIndex(organizationName, projectId, teamId, common.MinDateTime(now, estimationDate));

                await Task.WhenAll(earnedValue, costPerformanceIndex);

                return CostEstimationFormulas.Typical(costPerformanceIndex.Result, earnedValue.Result);
            }

            async Task<long> CalculateAtypical()
            {
                var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);
                var estimatedEarnedValue = CalculateEstimatedEarnedValue(organizationName, projectId, teamId, now, estimationDate);
                var earnedValue = CalculateReportedEarnedValue(organizationName, projectId, teamId, now);

                await Task.WhenAll(actualCost, estimatedEarnedValue, earnedValue);

                return CostEstimationFormulas.Atypical(actualCost.Result, estimatedEarnedValue.Result, earnedValue.Result);
            }
        }

        public struct CpiCriteria
        {
            public long TotalBudget { get; set; }
            public double TotalEffort { get; set; }
            public long Budget { get; set; }
            public double Effort { get; set; }
            public long Expenditure { get; set; }
        }

        public async Task<CpiCriteria> CalculateCpiCriteria(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now)
        {
            var start = await common.GetTeamStartDate(organizationName, projectId, teamId);
            var criteria = await CalculateCpiCriteria(organizationName, projectId, teamId, start, now);

            return criteria;
        }

        public async Task<CpiCriteria> CalculateCpiCriteria(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end)
        {
            var totalBudget = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);
            var budget = CalculateReportedEarnedValue(organizationName, projectId, teamId, start, end);
            var effort = CalculateReportedEffort(organizationName, projectId, teamId, start, end);
            var expenditure = CalculateActualCost(organizationName, projectId, teamId, start, end);

            await Task.WhenAll(budget, effort, expenditure, totalBudget, totalEffort);

            var criteria = new CpiCriteria()
            {
                TotalBudget = totalBudget.Result,
                TotalEffort = totalEffort.Result,
                Budget = budget.Result,
                Effort = effort.Result,
                Expenditure = expenditure.Result
            };

            return criteria;
        }

        public struct SpiCriteria
        {
            public double ProjectDuration { get; set; }
            public double TotalEffort { get; set; }
            public double ActualDuration { get; set; }
            public double EffortQuota { get; set; }
            public double CompletedEffort { get; set; }
        }

        public async Task<SpiCriteria> CalculateSpiCriteria(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now)
        {
            var start = await common.GetTeamStartDate(organizationName, projectId, teamId);
            var criteria = await CalculateSpiCriteria(organizationName, projectId, teamId, start, now);

            return criteria;
        }

        public async Task<SpiCriteria> CalculateSpiCriteria(
            string organizationName,
            string projectId,
            string teamId,
            DateTime start,
            DateTime end)
        {
            var projectDuration = CalculatePlannedDuration(organizationName, projectId, teamId);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);
            var team = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);
            var plannedValue = CalculatePlannedValue(organizationName, projectId, teamId, start, end);
            var actualEarnedValue = CalculateActualEarnedValue(organizationName, projectId, teamId, start, end);
            var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var effort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);

            await Task.WhenAll(team, plannedValue, actualEarnedValue, budgetAtCompletion, effort, totalEffort, projectDuration);

            var actualDuration = start.WorkingDaysUntil(end, team.Result);
            var effortQuota = (Convert.ToDouble(plannedValue.Result) / Convert.ToDouble(budgetAtCompletion.Result)) * effort.Result;
            var completedEffort = (Convert.ToDouble(actualEarnedValue.Result) / Convert.ToDouble(budgetAtCompletion.Result)) * effort.Result;

            var criteria = new SpiCriteria()
            {
                ProjectDuration = projectDuration.Result,
                TotalEffort = totalEffort.Result,
                ActualDuration = actualDuration,
                EffortQuota = effortQuota,
                CompletedEffort = completedEffort
            };
            return criteria;
        }
    }
}
