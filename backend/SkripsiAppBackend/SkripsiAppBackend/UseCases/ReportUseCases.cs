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
                    Expenditure = model.Expenditure
                };
            }
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
            var sprints = await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId);

            if (sprints.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            var startDate = (DateTime)sprints
                .FindAll(sprint => sprint.StartDate != null)
                .OrderBy(sprint => sprint.StartDate)
                .First().StartDate;
            var endDate = (DateTime)sprints
                .FindAll(sprint => sprint.EndDate != null)
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

        public async Task<ReportMetrics> CalculateReportMetrics(string organizationName, string projectId, string teamId, Report createdReport)
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


            var beforeMetrics = await CalculateCumulativeMetrics(
                organizationName,
                projectId,
                teamId,
                previousReports,
                EstimateAtCompletionFormulas.Basic,
                EstimateToCompletionFormulas.Derived);

            var afterMetrics = await CalculateCumulativeMetrics(
                organizationName,
                projectId,
                teamId,
                previousReports.Append(createdReport).ToList(),
                EstimateAtCompletionFormulas.Basic,
                EstimateToCompletionFormulas.Derived);

            return new ReportMetrics()
            {
                CumulativeMetrics = beforeMetrics,
                DeltaMetrics = afterMetrics - beforeMetrics
            };
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

            var team = await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);
            var workDays = await azureDevopsService.ReadTeamWorkDays(organizationName, projectId, teamId);

            var reportDuration = ((DateTime)report.StartDate).WorkingDaysUntil((DateTime)report.EndDate, workDays);
            var teamDuration = ((DateTime)report.StartDate).WorkingDaysUntil((DateTime)team.Deadline, workDays);
            var teamEffort = await teamUseCases.CalculateTeamEffort(organizationName, projectId, teamId);

            var plannedEffort = (reportDuration / teamDuration) * teamEffort;
            var completedEffort = await CalculateReportCompletedEffort(organizationName, projectId, teamId, (DateTime)report.StartDate, (DateTime)report.EndDate);

            if (!team.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var plannedValue = CalculateEffortValue((int)team.CostPerEffort, plannedEffort);
            var earnedValue = CalculateEffortValue((int)team.CostPerEffort, completedEffort);

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

            var cumulativeBasicMetricsList = (await Task.WhenAll(basicMetricsTasks)).ToList();

            var cumulativeBasicMetrics = cumulativeBasicMetricsList.Aggregate(BasicMetrics.GetEmpty(), (current, cumulative) => cumulative + current);

            var healthMetrics = new HealthMetrics()
            {
                CostVariance = cumulativeBasicMetrics.EarnedValue - cumulativeBasicMetrics.ActualCost,
                ScheduleVariance = cumulativeBasicMetrics.EarnedValue - cumulativeBasicMetrics.PlannedValue,
                CostPerformanceIndex = cumulativeBasicMetrics.ActualCost == 0 ? 0 : Convert.ToDouble(cumulativeBasicMetrics.EarnedValue) / Convert.ToDouble(cumulativeBasicMetrics.ActualCost),
                SchedulePerformanceIndex = cumulativeBasicMetrics.PlannedValue == 0 ? 0 : Convert.ToDouble(cumulativeBasicMetrics.EarnedValue) / Convert.ToDouble(cumulativeBasicMetrics.PlannedValue)
            };

            if (reports.Any(report => !report.StartDate.HasValue || !report.EndDate.HasValue))
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.REPORT_INCOMPLETE_INFORMATION);
            }

            var team = await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);
            var completedEffortTasks = reports.Select(async (report) => await CalculateReportCompletedEffort(
                organizationName,
                projectId,
                teamId,
                (DateTime)report.StartDate,
                (DateTime)report.EndDate
            ));
            var totalEffort = (await Task.WhenAll(completedEffortTasks))
                .ToList()
                .Aggregate(0d, (effort, total) => effort + total);

            if (!team.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_EFFORT_COST);
            }

            var budgetAtCompletion = Convert.ToInt32(Convert.ToDouble(team.CostPerEffort.Value) * totalEffort);

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
