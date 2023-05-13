using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.Calculations
{
    public class ReportCalculations
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;
        private readonly TeamEvmCalculations evm;

        public ReportCalculations(
            Database database,
            IAzureDevopsService azureDevops,
            TeamEvmCalculations evm)
        {
            this.database = database;
            this.azureDevops = azureDevops;
            this.evm = evm;
        }

        public struct AvailableReport
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public struct SingleReportMetrics
        {
            public Report Report { get; set; }
            public double CostPerformanceIndex { get; set; }
            public double SchedulePerformanceIndex { get; set; }
            public List<string> Errors { get; set; }
        }

        public struct Report
        {
            public int? Id { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? Expenditure { get; set; }

            public static Report FromModel(Persistence.Models.Report model)
            {
                return new Report()
                {
                    Id = model.Id,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    // TODO: Properly use long.
                    Expenditure = Convert.ToInt32(model.Expenditure)
                };
            }
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

            var reportMetrics = (await Task.WhenAll(reports
                .OrderBy(report => report.StartDate)
                .Reverse()
                .Select(async (report) =>
                {
                    var cpi = ExceptionHelpers.GetResultOrException(async () => await evm.CalculateCostPerformanceIndex(
                        organizationName,
                        projectId,
                        teamId,
                        report.StartDate,
                        report.EndDate
                    ));
                    var spi = ExceptionHelpers.GetResultOrException(async () => await evm.CalculateSchedulePerformanceIndex(
                        organizationName,
                        projectId,
                        teamId,
                        report.StartDate,
                        report.EndDate
                    ));

                    await Task.WhenAll(cpi, spi);

                    return new SingleReportMetrics()
                    {
                        Report = Report.FromModel(report),
                        CostPerformanceIndex = cpi.Result.Value,
                        SchedulePerformanceIndex = spi.Result.Value,
                        Errors = ExceptionHelpers.GetErrors(cpi.Result.Exception, spi.Result.Exception)
                    };
                })
            ))
            .ToList();

            return reportMetrics;
        }

        public async Task<List<AvailableReport>> ListAvailableReports(string organizationName, string projectId, string teamId)
        {
            var sprints = (await azureDevops.ReadTeamSprints(organizationName, projectId, teamId))
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

            return availableReports.OrderByDescending(report => report.StartDate).ToList();
        }
    }
}
