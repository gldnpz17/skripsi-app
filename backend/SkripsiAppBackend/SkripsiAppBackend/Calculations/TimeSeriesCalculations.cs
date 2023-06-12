using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.DateTimeService;
using SkripsiAppBackend.UseCases.Extensions;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Calculations.TeamEvmCalculations;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeams.TrackedTeamsRepository;

namespace SkripsiAppBackend.Calculations
{
    public class TimeSeriesCalculations
    {
        private readonly CommonCalculations common;
        private readonly TeamEvmCalculations evm;
        private readonly ReportCalculations reportCalc;
        private readonly MiscellaneousCalculations misc;
        private readonly IDateTimeService dateTime;
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;

        public TimeSeriesCalculations(
            CommonCalculations common,
            TeamEvmCalculations evm,
            ReportCalculations reportCalc,
            MiscellaneousCalculations misc,
            IDateTimeService dateTime,
            Database database,
            IAzureDevopsService azureDevops)
        {
            this.common = common;
            this.evm = evm;
            this.reportCalc = reportCalc;
            this.misc = misc;
            this.dateTime = dateTime;
            this.database = database;
            this.azureDevops = azureDevops;
        }

        public struct CpiChartItem
        {
            public DateTime Month { get; set; }
            public double CostPerformanceIndex { get; set; }
        }

        public async Task<List<CpiChartItem>> CalculateCpiChart(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);
            var reports = await database.Reports.ReadTeamReports(teamKey);

            if (reports.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.NO_REPORT);
            }

            var cpiTimeSeries = await Task.WhenAll(
                reports
                    .OrderBy(report => report.StartDate)
                    .Where(report => report.Expenditure != 0)
                    .Select(async (report) =>
                    {
                        var costPerformanceIndex = await evm.CalculateCostPerformanceIndex(organizationName, projectId, teamId, report.StartDate, report.EndDate);

                        return new CpiChartItem()
                        {
                            Month = report.StartDate,
                            CostPerformanceIndex = costPerformanceIndex
                        };
                    })
            );

            return cpiTimeSeries.ToList();
        }

        public struct BurndownChart
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public double TotalEffort { get; set; }
            public List<BurndownChartItem> Items { get; set; }
        }

        public struct BurndownChartItem
        {
            public DateTime Date { get; set; }
            public double Effort { get; set; }
            public double RemainingEffort { get; set; }
        }

        public async Task<BurndownChart> CalculateBurndownChart(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var sprints = azureDevops.ReadTeamSprints(organizationName, projectId, teamId);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);

            await Task.WhenAll(team, sprints, startDate, totalEffort);

            var endDate = team.Result.Deadline;

            var sprintEffortTasks = sprints.Result
                .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                .OrderBy(sprint => sprint.EndDate)
                .Select(async (sprint) =>
                {
                    var workItems = await azureDevops.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);
                    var completedWorkItems = workItems.Where(workItem => workItem.State == IAzureDevopsService.WorkItemState.Done).ToList();
                    var effort = common.CalculateTotalEffort(completedWorkItems);

                    return new BurndownChartItem()
                    {
                        Date = (DateTime)sprint.EndDate,
                        Effort = effort
                    };
                });

            var sprintEfforts = await Task.WhenAll(sprintEffortTasks);

            var sprintCumulativeEfforts = sprintEfforts.Aggregate(new List<BurndownChartItem>(), (sprints, sprint) =>
            {
                if (sprints.Count == 0)
                {
                    sprint.RemainingEffort = totalEffort.Result - sprint.Effort;
                    sprints.Add(sprint);
                    return sprints;
                }

                sprint.RemainingEffort = sprints.Last().RemainingEffort - sprint.Effort;
                sprints.Add(sprint);

                return sprints;
            });

            var burndownChart = new BurndownChart()
            {
                StartDate = startDate.Result,
                EndDate = (DateTime)endDate,
                TotalEffort = totalEffort.Result,
                Items = sprintCumulativeEfforts
            };

            return burndownChart;
        }

        public struct VelocityChartItem
        {
            public int Index { get; set; }
            public double Velocity { get; set; }
            public double Effort { get; set; }
            public double Duration { get; set; }
            public double MinimumAverageVelocity { get; set; }
        }

        private struct VelocityChartAggregation
        {
            public VelocityChartAggregation(double effort, double duration, List<VelocityChartItem> items)
            {
                Effort = effort;
                Duration = duration;
                Items = items;
            }

            public double Effort { get; set; }
            public double Duration { get; set; }
            public List<VelocityChartItem> Items { get; set; }
        }

        public async Task<List<VelocityChartItem>> CalculateVelocityChart(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var team = database.TrackedTeams.ReadByKey(teamKey);
            var sprints = azureDevops.ReadTeamSprints(organizationName, projectId, teamId);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);
            var workDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);

            await Task.WhenAll(team, sprints, startDate, totalEffort, workDays);

            var endDate = team.Result.Deadline;
            var totalDuration = startDate.Result.WorkingDaysUntil((DateTime)endDate, workDays.Result);

            var itemTasks = sprints.Result
                .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                .OrderBy(sprint => sprint.EndDate)
                .Select(async (sprint, index) =>
                {
                    var workItems = await azureDevops.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);
                    var completedWorkItems = workItems.Where(workItem => workItem.State == IAzureDevopsService.WorkItemState.Done).ToList();

                    var effort = common.CalculateTotalEffort(completedWorkItems);
                    var duration = ((DateTime)sprint.StartDate).WorkingDaysUntil((DateTime)sprint.EndDate, workDays.Result);

                    var velocity = effort / duration;

                    return new VelocityChartItem()
                    {
                        Index = index,
                        Duration = duration,
                        Effort = effort,
                        Velocity = velocity
                    };
                });

            var items = (await Task.WhenAll(itemTasks)).OrderBy(item => item.Index).ToList();

            var remainingEffort = totalEffort.Result;
            var remainingDuration = totalDuration;

            for (int i = 0; i < items.Count; i++)
            {
                var minAverageVelocity = remainingEffort / remainingDuration;

                // Structs are immutable.
                // See : https://stackoverflow.com/questions/51526/changing-the-value-of-an-element-in-a-list-of-structs
                var item = items[i];
                item.MinimumAverageVelocity = minAverageVelocity;
                items[i] = item;

                remainingEffort -= item.Effort;
                remainingDuration -= item.Duration;
            }

            return items;
        }

        public struct WorkCostChartItem
        {
            public DateTime Month { get; set; }
            public long Expenditure { get; set; }
            public double Effort { get; set; }
            public double WorkCost { get; set; }
            public double MaximumAverageWorkCost { get; set; }

        }

        public async Task<List<WorkCostChartItem>> CalculateWorkCostChart(
            string organizationName,
            string projectId,
            string teamId)
        {
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var reports = database.Reports.ReadTeamReports(teamKey);
            var totalEffort = common.CalculateTeamTotalEffort(organizationName, projectId, teamId);
            var budget = evm.CalculateBudgetAtCompletion(organizationName, projectId, teamId);

            await Task.WhenAll(reports, totalEffort, budget);

            if (reports.Result.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.NO_REPORT);
            }

            var itemsTask = reports.Result
                .OrderBy(report => report.StartDate)
                .Select(async (report) =>
                {
                    var sprints = await common.ReadAdjustedSprints(
                        organizationName,
                        projectId,
                        teamId,
                        report.StartDate,
                        report.EndDate,
                        (workItems) => workItems.Where(workItem => workItem.State == IAzureDevopsService.WorkItemState.Done).ToList());
                    var totalEffort = common.CalculateTotalEffort(sprints);

                    return new WorkCostChartItem()
                    {
                        Month = report.StartDate,
                        Effort = totalEffort,
                        Expenditure = report.Expenditure,
                        WorkCost = report.Expenditure / totalEffort
                    };
                });

            var items = (await Task.WhenAll(itemsTask)).ToList();

            var remainingBudget = budget.Result;
            var remainingEffort = totalEffort.Result;
            
            for (int i = 0; i < items.Count; i++)
            {
                var maxWorkCost = remainingBudget / remainingEffort;

                var item = items[i];
                item.MaximumAverageWorkCost = maxWorkCost;
                items[i] = item;

                remainingEffort -= item.Effort;
                remainingBudget -= item.Expenditure;
            }

            return items;
        }

        public struct MilestoneChartItem
        {
            public DateTime Month { get; set; }
            public double RemainingWorkPercentage { get; set; }
            public double RemainingBudgetPercentage { get; set; }
            public double IdealRemainingPercentage { get; set; }
            public bool IsForecast { get; set; }
        }

        public async Task<List<MilestoneChartItem>> CalculateMilestoneChart(
            string organizationName,
            string projectId,
            string teamId,
            EstimateAtCompletionFormulas eacFormula)
        {
            var now = dateTime.GetNow();
            
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var estimatedDeadline = evm.CalculateEstimatedCompletionDate(organizationName, projectId, teamId, now);
            var budgetAtCompletion = evm.CalculateBudgetAtCompletion(organizationName, projectId, teamId);

            await Task.WhenAll(startDate, estimatedDeadline, budgetAtCompletion);

            var reports = await reportCalc.ListAvailableReports(startDate.Result, estimatedDeadline.Result);

            var milestoneTasks = reports.Select(async (report) =>
            {
                var earnedValue = evm.CalculateEstimatedEarnedValue(organizationName, projectId, teamId, now, report.EndDate.Clamp(startDate.Result, estimatedDeadline.Result));
                var actualCost = evm.CalculateEstimatedActualCost(organizationName, projectId, teamId, now, report.EndDate, eacFormula);
                var plannedValue = evm.CalculatePlannedValue(organizationName, projectId, teamId, report.EndDate);

                await Task.WhenAll(earnedValue, actualCost, plannedValue);

                var remainingWorkPercentage = 100 - (Convert.ToDouble(earnedValue.Result) / Convert.ToDouble(budgetAtCompletion.Result)) * 100;
                var remainingBudgetPercentage = 100 - (Convert.ToDouble(actualCost.Result) / Convert.ToDouble(budgetAtCompletion.Result)) * 100;
                var idealRemainingPercentage = 100 - (Convert.ToDouble(plannedValue.Result) / Convert.ToDouble(budgetAtCompletion.Result)) * 100;

                return new MilestoneChartItem()
                {
                    Month = report.EndDate,
                    RemainingWorkPercentage = remainingWorkPercentage,
                    RemainingBudgetPercentage = remainingBudgetPercentage,
                    IdealRemainingPercentage = idealRemainingPercentage,
                    IsForecast = report.EndDate > now
                };
            });

            var milestones = (await Task.WhenAll(milestoneTasks)).OrderBy(milestone => milestone.Month).ToList();

            return milestones;
        }
    }
}
