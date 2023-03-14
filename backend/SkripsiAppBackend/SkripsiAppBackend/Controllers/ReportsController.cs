using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.UseCases;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : Controller
    {
        private readonly Database database;
        private readonly ReportUseCases reportUseCases;

        public ReportsController(Database database, ReportUseCases reportUseCases)
        {
            this.database = database;
            this.reportUseCases = reportUseCases;
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
                    Expenditure = report.Expenditure
                };
            }
        }

        public struct CreateReportDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports")]
        [HttpPost]
        public async Task<ActionResult> CreateReport(
            [FromRoute]string organizationName,
            [FromRoute]string projectId,
            [FromRoute]string teamId,
            [FromBody]CreateReportDto dto)
        {
            var teamKey = new TrackedTeamsRepository.TrackedTeamKey()
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            await database.Reports.CreateReport(teamKey, dto.StartDate, dto.EndDate);

            return Ok();
        }

        public struct PatchReportDto
        {
            public int? Expenditure { get; set; }
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
        public async Task<ActionResult<List<Report>>> ReadTeamReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var teamKey = new TrackedTeamsRepository.TrackedTeamKey()
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            var reports = await database.Reports.ReadTeamReports(teamKey);

            return reports
                .Select(report => Report.FromPersistenceModel(report))
                .ToList();
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/available")]
        [HttpGet]
        public async Task<List<ReportUseCases.AvailableReport>> ReadAllAvailableReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            return await reportUseCases.ListAvailableReports(organizationName, projectId, teamId);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/timespan-sprints")]
        [HttpGet]
        public async Task<List<ReportUseCases.ReportSprint>> ReadTimespanSprints(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            return await reportUseCases.GetTimespanSprints(organizationName, projectId, teamId, start, end);
        }
    }
}
