using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.UseCases;
using System.Runtime.CompilerServices;
using static SkripsiAppBackend.UseCases.MetricUseCases;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : Controller
    {
        private readonly Database database;
        private readonly MetricUseCases metricUseCases;

        public ReportsController(Database database, MetricUseCases metricUseCases)
        {
            this.database = database;
            this.metricUseCases = metricUseCases;
        }

        public struct Team
        {
            public string OrganizationName { get; set; }
            public string ProjectId { get; set; }
            public string TeamId { get; set; }
        }

        public struct Report
        {
            public int Id { get; set; }
            public Team Team { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }

            public int Expenditure { get; set; }
            public static Report FromPersistenceModel(Persistence.Models.Report report)
            {
                return new Report()
                {
                    Id = report.Id,
                    Team = new Team()
                    {
                        OrganizationName = report.OrganizationName,
                        ProjectId = report.ProjectId,
                        TeamId = report.TeamId,
                    },
                    StartDate = report.StartDate,
                    EndDate = report.EndDate,
                    // TODO: Properly use long.
                    Expenditure = Convert.ToInt32(report.Expenditure)
                };
            }
        }

        public struct CreateReportDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Expenditure { get; set; }

            public MetricUseCases.Report ToUseCaseModel()
            {
                return new MetricUseCases.Report()
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Expenditure = Expenditure
                };
            }
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports")]
        [HttpPost]
        public async Task<ActionResult> CreateReport(
            [FromRoute]string organizationName,
            [FromRoute]string projectId,
            [FromRoute]string teamId,
            [FromBody]CreateReportDto dto)
        {
            await metricUseCases.CreateReport(organizationName, projectId, teamId, dto.ToUseCaseModel());

            return Ok();
        }

        public struct PatchReportDto
        {
            public int? Expenditure { get; set; }
        }

        [HttpGet("{reportId}")]
        public async Task<Report> ReadReportById([FromRoute] int reportId)
        {
            var report = await database.Reports.ReadReport(reportId);

            return Report.FromPersistenceModel(report);
        }

        [HttpPatch("{reportId}")]
        public async Task<ActionResult> PatchReport([FromRoute] int reportId, [FromBody] PatchReportDto dto)
        {
            if (dto.Expenditure.HasValue)
            {
                await database.Reports.UpdateExpenditure(reportId, (int)dto.Expenditure);
            }

            return Ok();
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports")]
        [HttpGet]
        public async Task<ActionResult<List<SingleReportMetrics>>> ReadTeamReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            return await metricUseCases.ListExistingReports(organizationName, projectId, teamId);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/available")]
        [HttpGet]
        public async Task<List<MetricUseCases.AvailableReport>> ReadAllAvailableReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)  
        {
            return await metricUseCases.ListAvailableReports(organizationName, projectId, teamId);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/timespan-sprints")]
        [HttpGet]
        public async Task<List<MetricUseCases.ReportSprint>> ReadTimespanSprints(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            return await metricUseCases.GetTimespanSprints(organizationName, projectId, teamId, start, end);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/new-report-metrics")]
        [HttpGet]
        public async Task<MetricUseCases.ReportMetrics> ReadNewReportMetrics(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] int expenditure)
        {
            return await metricUseCases.CalculateReportMetrics(
                organizationName,
                projectId,
                teamId, new MetricUseCases.Report()
                {
                    StartDate = start,
                    EndDate = end,
                    Expenditure = expenditure
                });
        }
    }
}
