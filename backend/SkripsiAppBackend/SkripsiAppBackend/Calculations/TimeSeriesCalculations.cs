using Flurl.Http.Configuration;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.Calculations
{
    public class TimeSeriesCalculations
    {
        private readonly CommonCalculations common;
        private readonly TeamEvmCalculations evm;
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;

        public TimeSeriesCalculations(
            CommonCalculations common,
            TeamEvmCalculations evm,
            Database database,
            IAzureDevopsService azureDevops)
        {
            this.common = common;
            this.evm = evm;
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
    }
}
