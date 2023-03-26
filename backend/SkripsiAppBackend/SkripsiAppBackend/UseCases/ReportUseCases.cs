using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Controllers;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
using System.ComponentModel;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.UseCases
{
    public class ReportUseCases
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevopsService;
        private readonly TeamUseCases teamUseCases;

        public ReportUseCases(
            Database database,
            IAzureDevopsService azureDevopsService,
            TeamUseCases teamUseCases)
        {
            this.database = database;
            this.azureDevopsService = azureDevopsService;
            this.teamUseCases = teamUseCases;
        }

        public struct AvailableReport
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public struct ReportSprint
        {
            public IAzureDevopsService.Sprint Sprint { get; set; }
            public DateTime AccountedStartDate { get; set; }
            public DateTime AccountedEndDate { get; set; }
            public double AccountedEffort { get; set; }
            public double AccountedWorkFactor { get; set; }
        }

        public interface IMetrics<TMetric>
        {
            // TODO: Probably should cache some reflection values?
            protected static TMetric EveryProperty(TMetric a, TMetric b, Func<dynamic, dynamic, dynamic> operate)
            {
                var metricType = typeof(TMetric);
                var properties = metricType.GetProperties().ToList();

                var result = (TMetric)Activator.CreateInstance(metricType);

                properties.ForEach(property =>
                {
                    var firstValue = property.GetValue(a);
                    var secondValue = property.GetValue(b);

                    var resultValue = (object)Convert.ChangeType(operate.Invoke(firstValue, secondValue), property.PropertyType);

                    object boxedResult = result;
                    property.SetValue(boxedResult, resultValue);
                    result = (TMetric)boxedResult;
                });

                return result;
            }
        }


        public struct BasicMetrics : IMetrics<BasicMetrics>
        {
            public int PlannedValue { get; set; }
            public int EarnedValue { get; set; }
            public int ActualCost { get; set; }

            public static BasicMetrics operator +(BasicMetrics a, BasicMetrics b)
            {
                return IMetrics<BasicMetrics>.EveryProperty(a, b, (a, b) => a + b);
            }

            public static BasicMetrics operator -(BasicMetrics a, BasicMetrics b)
            {
                return IMetrics<BasicMetrics>.EveryProperty(a, b, (a, b) => a - b);
            }

            public static BasicMetrics GetEmpty()
            {
                return new BasicMetrics()
                {
                    PlannedValue = 0,
                    EarnedValue = 0,
                    ActualCost = 0
                };
            }
        }

        public struct HealthMetrics : IMetrics<HealthMetrics>
        {
            public int CostVariance { get; set; }
            public int ScheduleVariance { get; set; }
            public double CostPerformanceIndex { get; set; }
            public double SchedulePerformanceIndex { get; set; }

            public static HealthMetrics operator +(HealthMetrics a, HealthMetrics b)
            {
                return IMetrics<HealthMetrics>.EveryProperty(a, b, (a, b) => a + b);
            }

            public static HealthMetrics operator -(HealthMetrics a, HealthMetrics b)
            {
                return IMetrics<HealthMetrics>.EveryProperty(a, b, (a, b) => a - b);
            }
        }

        public struct ForecastMetrics : IMetrics<ForecastMetrics>
        {
            public int BudgetAtCompletion { get; set; }
            public int EstimateToCompletion { get; set; }
            public int EstimateAtCompletion { get; set; }
            public int VarianceAtCompletion { get; set; }

            public static ForecastMetrics operator +(ForecastMetrics a, ForecastMetrics b)
            {
                return IMetrics<ForecastMetrics>.EveryProperty(a, b, (a, b) => a + b);
            }

            public static ForecastMetrics operator -(ForecastMetrics a, ForecastMetrics b)
            {
                return IMetrics<ForecastMetrics>.EveryProperty(a, b, (a, b) => a - b);
            }
        }

        public struct MetricsCollection : IMetrics<MetricsCollection>
        {
            public BasicMetrics BasicMetrics { get; set; }
            public HealthMetrics HealthMetrics { get; set; }
            public ForecastMetrics ForecastMetrics { get; set; }

            public static MetricsCollection operator +(MetricsCollection a, MetricsCollection b)
            {
                return IMetrics<MetricsCollection>.EveryProperty(a, b, (a, b) => a + b);
            }

            public static MetricsCollection operator -(MetricsCollection a, MetricsCollection b)
            {
                return IMetrics<MetricsCollection>.EveryProperty(a, b, (a, b) => a - b);
            }
        }

        public struct Report
        {
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? Expenditure { get; set; }

            public static Report FromModel(Persistence.Models.Report model)
            {
                return new Report()
                {
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    // TODO: Properly use long.
                    Expenditure = Convert.ToInt32(model.Expenditure)
                };
            }
        }

        public struct SingleReportMetrics
        {
            public Report Report { get; set; }
            public HealthMetrics HealthMetrics { get; set; }
        }

        public struct ReportMetrics
        {
            public MetricsCollection CumulativeMetrics { get; set; }
            public MetricsCollection DeltaMetrics { get; set; }
        }

        public enum EstimateToCompletionFormulas
        {
            Derived,
            Atypical,
            Typical
        }

        public enum EstimateAtCompletionFormulas
        {
            Derived,
            Basic,
            Atypical,
            Typical
        }

        public async Task<List<ReportSprint>> GetTimespanSprints(string organizationName, string projectId, string teamId, DateTime startDate, DateTime endDate)
        {
            var sprints = await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId);

            var accountedSprints = sprints
                .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                .Where(sprint =>
                    sprint.StartDate >= startDate && sprint.EndDate <= endDate ||
                    sprint.StartDate <= startDate && sprint.EndDate >= startDate ||
                    sprint.StartDate <= endDate && sprint.EndDate >= endDate
                )
                .Select(async (sprint) =>
                {
                    DateTime sprintStartDate = (DateTime)sprint.StartDate;
                    DateTime sprintEndDate = (DateTime)sprint.EndDate;

                    var accountedStartDate = new DateTime(Math.Max(sprintStartDate.Ticks, startDate.Ticks));
                    var accountedEndDate = new DateTime(Math.Min(sprintEndDate.Ticks, endDate.Ticks));

                    var accountedWorkFactor = (accountedEndDate - accountedStartDate).TotalDays / (sprintEndDate - sprintStartDate).TotalDays;

                    var sprintWorkItems = await azureDevopsService.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);
                    var totalEffort = teamUseCases.CalculateTotalEffort(sprintWorkItems);

                    return new ReportSprint()
                    {
                        Sprint = sprint,
                        AccountedWorkFactor = accountedWorkFactor,
                        AccountedStartDate = accountedStartDate,
                        AccountedEndDate = accountedEndDate,
                        AccountedEffort = totalEffort * accountedWorkFactor
                    };
                });

            await Task.WhenAll(accountedSprints);

            return accountedSprints.Select(task => task.Result).ToList();
        }

        public async Task<List<AvailableReport>> ListAvailableReports(string organizationName, string projectId, string teamId)
        {
            var sprints = (await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId))
                .FindAll(sprint => sprint.StartDate != null);

            if (sprints.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            var startDate = (DateTime)sprints
                .OrderBy(sprint => sprint.StartDate)
                .First().StartDate;
            var endDate = (DateTime)sprints
                .OrderBy(sprint => sprint.EndDate)
                .Last().EndDate;

            var existingReports = await database.Reports.ReadTeamReports(new TrackedTeamKey()
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId,
            });

            var currentMonthBeginning = new DateTime(startDate.Year, startDate.Month, 1);

            var availableReports = new List<AvailableReport>();

            while (currentMonthBeginning < endDate)
            {
                var currentMonthEnd = currentMonthBeginning.AddMonths(1).AddSeconds(-1);

                var collidingReport = existingReports
                    .FirstOrDefault(report =>
                        (report.StartDate >= currentMonthBeginning && report.EndDate <= currentMonthEnd) ||
                        (report.StartDate <= currentMonthBeginning && report.EndDate >= currentMonthEnd) ||
                        (report.StartDate <= currentMonthEnd && report.EndDate >= currentMonthBeginning)
                    );

                if (collidingReport == null)
                {
                    availableReports.Add(new AvailableReport()
                    {
                        StartDate = currentMonthBeginning,
                        EndDate = currentMonthEnd
                    });
                }

                currentMonthBeginning = currentMonthBeginning.AddMonths(1);
            }

            return availableReports;
        }

        public async Task<List<SingleReportMetrics>> ListExistingReports(string organizationName, string projectId, string teamId)
        {
            var teamKey = new TrackedTeamKey()
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            var reports = await database.Reports.ReadTeamReports(teamKey);

            var reportMetrics = (await Task.WhenAll(reports.Select(async (report) =>
            {
                var basicMetrics = await CalculateReportBasicMetrics(organizationName, projectId, teamId, Report.FromModel(report));
                var healthMetrics = CalculateHealthMetrics(basicMetrics.PlannedValue, basicMetrics.EarnedValue, basicMetrics.ActualCost);

                return new SingleReportMetrics()
                {
                    Report = Report.FromModel(report),
                    HealthMetrics = healthMetrics
                };
            })))
            .ToList();

            return reportMetrics;
        }

        public async Task<ReportMetrics> CalculateReportMetrics(
            string organizationName,
            string projectId,
            string teamId,
            Report createdReport)
        {
            var previousReports = (await database.Reports.ReadTeamReports(new TrackedTeamKey()
                {
                    OrganizationName = organizationName,
                    ProjectId = projectId,
                    TeamId = teamId,
                }))
                .Where(report => report.EndDate < createdReport.StartDate)
                .Select(report => Report.FromModel(report))
                .ToList();


            var beforeMetrics = CalculateCumulativeMetrics(
                organizationName,
                projectId,
                teamId,
                previousReports,
                EstimateAtCompletionFormulas.Basic,
                EstimateToCompletionFormulas.Derived);

            var afterMetrics = CalculateCumulativeMetrics(
                organizationName,
                projectId,
                teamId,
                previousReports.Append(createdReport).ToList(),
                EstimateAtCompletionFormulas.Basic,
                EstimateToCompletionFormulas.Derived);

            await Task.WhenAll(beforeMetrics, afterMetrics);

            return new ReportMetrics()
            {
                CumulativeMetrics = beforeMetrics.Result,
                DeltaMetrics = afterMetrics.Result - beforeMetrics.Result
            };
        }

        public async Task CreateReport(
            string organizationName,
            string projectId,
            string teamId,
            Report report)
        {
            database.Reports.CreateReport(
                new TrackedTeamKey()
                {
                    OrganizationName = organizationName,
                    ProjectId = projectId,
                    TeamId = teamId
                },
                (DateTime)report.StartDate,
                (DateTime)report.EndDate,
                (int)report.Expenditure
            );
        }

        public async Task<MetricsCollection> CalculateMetricsOverview()
        {
            throw new NotImplementedException();
        }

        public async Task<List<ReportMetrics>> CalculateTimelineMetrics()
        {
            throw new NotImplementedException();
        }

        public async Task<BasicMetrics> CalculateReportBasicMetrics(string organizationName, string projectId, string teamId, Report report)
        {
            if (!report.StartDate.HasValue || !report.EndDate.HasValue || !report.Expenditure.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.REPORT_INCOMPLETE_INFORMATION);
            }

            var team = database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);
            var workDays = azureDevopsService.ReadTeamWorkDays(organizationName, projectId, teamId);
            var teamEffort = teamUseCases.CalculateTeamEffort(organizationName, projectId, teamId);
            var completedEffort = CalculateReportCompletedEffort(organizationName, projectId, teamId, (DateTime)report.StartDate, (DateTime)report.EndDate);

            await Task.WhenAll(team, workDays, teamEffort, completedEffort);

            var reportDuration = ((DateTime)report.StartDate).WorkingDaysUntil((DateTime)report.EndDate, workDays.Result);
            var teamDuration = ((DateTime)report.StartDate).WorkingDaysUntil((DateTime)team.Result.Deadline, workDays.Result);
            

            var plannedEffort = (reportDuration / teamDuration) * teamEffort.Result;
            
            if (!team.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var plannedValue = CalculateEffortValue((int)team.Result.CostPerEffort, plannedEffort);
            var earnedValue = CalculateEffortValue((int)team.Result.CostPerEffort, completedEffort.Result);

            return new BasicMetrics()
            {
                PlannedValue = plannedValue,
                EarnedValue = earnedValue,
                ActualCost = (int)report.Expenditure
            };
        }

        public async Task<MetricsCollection> CalculateCumulativeMetrics(
            string organizationName,
            string projectId,
            string teamId,
            List<Report> reports,
            EstimateAtCompletionFormulas eacFormula,
            EstimateToCompletionFormulas etcFormula)
        {
            var basicMetricsTasks = reports.Select(async (report) => await CalculateReportBasicMetrics(
                organizationName,
                projectId,
                teamId,
                report
            ));
            var allBasicMetricsTasks = Task.WhenAll(basicMetricsTasks);

            var team = database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);

            var completedEffortTasks = reports.Select(async (report) => await CalculateReportCompletedEffort(
                organizationName,
                projectId,
                teamId,
                (DateTime)report.StartDate,
                (DateTime)report.EndDate
            ));
            var allCompletedEffortTasks = Task.WhenAll(completedEffortTasks);

            await Task.WhenAll(allBasicMetricsTasks, team, allCompletedEffortTasks);

            var cumulativeBasicMetrics = allBasicMetricsTasks.Result
                .Aggregate(BasicMetrics.GetEmpty(), (current, cumulative) => cumulative + current);

            var healthMetrics = CalculateHealthMetrics(
                cumulativeBasicMetrics.EarnedValue,
                cumulativeBasicMetrics.EarnedValue,
                cumulativeBasicMetrics.ActualCost
            );

            if (reports.Any(report => !report.StartDate.HasValue || !report.EndDate.HasValue))
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.REPORT_INCOMPLETE_INFORMATION);
            }

            var totalEffort = allCompletedEffortTasks.Result
                .Aggregate(0d, (effort, total) => effort + total);

            if (!team.Result.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var budgetAtCompletion = Convert.ToInt32(Convert.ToDouble(team.Result.CostPerEffort.Value) * totalEffort);

            if (eacFormula == EstimateAtCompletionFormulas.Derived && etcFormula == EstimateToCompletionFormulas.Derived)
            {
                throw new Exception("EAC and ETC formulas can't both be of type 'derived' at the same time.");
            }

            var estimateToCompletion = 0;
            var estimateAtCompletion = 0;

            if (etcFormula == EstimateToCompletionFormulas.Derived)
            {
                estimateAtCompletion = CalculateEstimateAtCompletion();
                estimateToCompletion = estimateAtCompletion - cumulativeBasicMetrics.ActualCost;
            }
            else if (eacFormula == EstimateAtCompletionFormulas.Derived)
            {
                estimateToCompletion = CalculateEstimateToCompletion();
                estimateAtCompletion = estimateToCompletion + cumulativeBasicMetrics.ActualCost;
            }
            else
            {
                estimateToCompletion = CalculateEstimateToCompletion();
                estimateAtCompletion = CalculateEstimateAtCompletion();
            }

            int CalculateEstimateToCompletion()
            {
                return etcFormula switch
                {
                    EstimateToCompletionFormulas.Atypical => budgetAtCompletion - cumulativeBasicMetrics.EarnedValue,
                    EstimateToCompletionFormulas.Typical => healthMetrics.CostPerformanceIndex == 0 ? 0 : Convert.ToInt32(Convert.ToDouble(budgetAtCompletion - cumulativeBasicMetrics.EarnedValue) / healthMetrics.CostPerformanceIndex),
                    _ => throw new Exception("Can't calculate using the derived formula.")
                };
            }

            int CalculateEstimateAtCompletion()
            {
                return eacFormula switch
                {
                    // TODO: Hey, shouldn't we be using longs for money instead of 32-bit ints?
                    EstimateAtCompletionFormulas.Basic => healthMetrics.CostPerformanceIndex == 0 ? 0 : Convert.ToInt32(Convert.ToDouble(budgetAtCompletion) / healthMetrics.CostPerformanceIndex),
                    EstimateAtCompletionFormulas.Atypical => cumulativeBasicMetrics.ActualCost + budgetAtCompletion - cumulativeBasicMetrics.EarnedValue,
                    EstimateAtCompletionFormulas.Typical => healthMetrics.CostPerformanceIndex == 0 ? 0 : cumulativeBasicMetrics.ActualCost + Convert.ToInt32(Convert.ToDouble(budgetAtCompletion - cumulativeBasicMetrics.EarnedValue) / healthMetrics.CostPerformanceIndex),
                    _ => throw new Exception("Can't calculate using the derived formula.")
                };
            }

            var forecastMetrics = new ForecastMetrics()
            {
                BudgetAtCompletion = budgetAtCompletion,
                EstimateToCompletion = estimateToCompletion,
                EstimateAtCompletion = estimateAtCompletion,
                VarianceAtCompletion = budgetAtCompletion - estimateAtCompletion
            };

            return new MetricsCollection()
            {
                BasicMetrics = cumulativeBasicMetrics,
                HealthMetrics = healthMetrics,
                ForecastMetrics = forecastMetrics
            };
        }

        private HealthMetrics CalculateHealthMetrics(int plannedValue, int earnedValue, int actualCost)
        {
            return new HealthMetrics()
            {
                CostVariance = earnedValue - actualCost,
                ScheduleVariance = earnedValue - plannedValue,
                CostPerformanceIndex = actualCost == 0 ? 0 : Convert.ToDouble(earnedValue) / Convert.ToDouble(actualCost),
                SchedulePerformanceIndex = plannedValue == 0 ? 0 : Convert.ToDouble(earnedValue) / Convert.ToDouble(plannedValue)
            };
        }

        private async Task<double> CalculateReportCompletedEffort(string organizationName, string projectId, string teamId, DateTime startDate, DateTime endDate)
        {
            var sprints = await GetTimespanSprints(organizationName, projectId, teamId, startDate, endDate);
            var completedEffort = sprints.Aggregate(0d, (total, sprint) => total + sprint.AccountedEffort);
            return completedEffort;
        }

        private int CalculateEffortValue(int costPerEffort, double totalEffort)
        {
            return Convert.ToInt32(Convert.ToDouble(costPerEffort) * totalEffort);
        }
    }
}
