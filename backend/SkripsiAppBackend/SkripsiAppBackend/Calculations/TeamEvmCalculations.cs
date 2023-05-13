﻿using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

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
            var estimateAtCompletion = CalculateEstimateAtCompletion(organizationName, projectId, teamId, now, eacFormula);
            var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);

            await Task.WhenAll(estimateAtCompletion, actualCost);

            return estimateAtCompletion.Result - actualCost.Result;
        }

        public async Task<long> CalculateEstimateAtCompletion(string organizationName, string projectId, string teamId, DateTime now, EstimateAtCompletionFormulas eacFormula)
        {
            return eacFormula switch
            {
                EstimateAtCompletionFormulas.Typical => await CalculateBasic(),
                EstimateAtCompletionFormulas.Typical2 => await CalculateTypical2(),
                EstimateAtCompletionFormulas.Atypical => await CalculateAtypical(),
                _ => throw new Exception("Unknown formula")
            };

            async Task<long> CalculateBasic()
            {
                var costPerformanceIndex = CalculateCostPerformanceIndex(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);

                await Task.WhenAll(costPerformanceIndex, budgetAtCompletion);

                return Convert.ToInt64(Convert.ToDouble(budgetAtCompletion.Result) / costPerformanceIndex.Result);
            }

            async Task<long> CalculateAtypical()
            {
                var actualCost = CalculateActualCost(organizationName, projectId, teamId, now);
                var budgetAtCompletion = CalculateBudgetAtCompletion(organizationName, projectId, teamId);
                var earnedValue = CalculateReportedEarnedValue(organizationName, projectId, teamId, now);

                await Task.WhenAll(actualCost, budgetAtCompletion, earnedValue);

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

        public async Task<double> CalculateSchedulePerformanceIndex(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var actualEarnedValueTask = CalculateActualEarnedValue(organizationName, projectId, teamId, start, end);
            var plannedValueTask = CalculatePlannedValue(organizationName, projectId, teamId, start, end);

            await Task.WhenAll(actualEarnedValueTask, plannedValueTask);

            var schedulePerformanceIndex = Convert.ToDouble(actualEarnedValueTask.Result) / Convert.ToDouble(plannedValueTask.Result);

            return schedulePerformanceIndex;
        }

        public async Task<double> CalculateCostPerformanceIndex(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateCostPerformanceIndex(organizationName, projectId, teamId, DateTime.MinValue, now);
        }
        
        public async Task<double> CalculateCostPerformanceIndex(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var reportedEarnedValueTask = CalculateReportedEarnedValue(organizationName, projectId, teamId, start, end);
            var actualCostTask = CalculateActualCost(organizationName, projectId, teamId, start, end);

            await Task.WhenAll(reportedEarnedValueTask, actualCostTask);

            if (actualCostTask.Result == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.ZERO_EXPENDITURE);
            }

            var costPerformanceIndex = Convert.ToDouble(reportedEarnedValueTask.Result) / Convert.ToDouble(actualCostTask.Result);

            return costPerformanceIndex;
        }

        public async Task<long> CalculateActualCost(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateActualCost(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateActualCost(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var reports = await database.Reports.ReadTeamReports(teamKey);
            var actualCost = reports
                .Where(report => report.StartDate >= start && report.EndDate <= end)
                .Aggregate(0L, (total, report) => total + report.Expenditure);

            return actualCost;
        }

        public async Task<long> CalculatePlannedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            var startDate = await common.GetTeamStartDate(organizationName, projectId, teamId);
            return await CalculatePlannedValue(organizationName, projectId, teamId, startDate, now);
        }

        public async Task<long> CalculatePlannedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
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
            var actualDuration = start.Clamp(startDate.Result, deadline).WorkingDaysUntil(end, workingDays.Result);

            var plannedValue = (Convert.ToInt64(actualDuration) * budgetAtCompletion.Result) / Convert.ToInt64(projectDuration);

            return plannedValue;
        }

        public async Task<long> CalculateActualEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateActualEarnedValue(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateActualEarnedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
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
                .Select(sprint => common.AdjustSprint(
                    sprint.Sprint,
                    sprint.WorkItems.Where(workItem => workItem.State == WorkItemState.Done).ToList(),
                    start,
                    end
                ))
                .Aggregate(0d, (totalEffort, sprint) => totalEffort + sprint.Effort);

            if (!team.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var actualEarnedValue = Convert.ToInt64(completedEffort) * (long)team.Result.CostPerEffort;

            return actualEarnedValue;
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime now)
        {
            return await CalculateReportedEarnedValue(organizationName, projectId, teamId, DateTime.MinValue, now);
        }

        public async Task<long> CalculateReportedEarnedValue(string organizationName, string projectId, string teamId, DateTime start, DateTime end)
        {
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

                        return common.AdjustSprint(sprint, workItems.Where(workItem => workItem.State == WorkItemState.Done).ToList(), report.StartDate, report.EndDate);
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