using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Calculations;
using SkripsiAppBackend.Common.Authorization;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.UseCases;
using System.Runtime.CompilerServices;
using static SkripsiAppBackend.Calculations.CommonCalculations;
using static SkripsiAppBackend.Calculations.ReportCalculations;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : Controller
    {
        private readonly Database database;
        private readonly IAuthorizationService authorizationService;
        private readonly ReportCalculations reportCalculations;
        private readonly CommonCalculations commonCalculations;

        public ReportsController(
            Database database,
            IAuthorizationService authorizationService,
            ReportCalculations reportCalculations,
            CommonCalculations commonCalculations)
        {
            this.database = database;
            this.authorizationService = authorizationService;
            this.reportCalculations = reportCalculations;
            this.commonCalculations = commonCalculations;
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
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports")]
        [HttpPost]
        public async Task<ActionResult> CreateReport(
            [FromRoute]string organizationName,
            [FromRoute]string projectId,
            [FromRoute]string teamId,
            [FromBody]CreateReportDto dto)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            await database.Reports.CreateReport(teamKey, dto.StartDate, dto.EndDate, dto.Expenditure);

            return Ok();
        }

        public struct PatchReportDto
        {
            public int? Expenditure { get; set; }
        }

        [HttpGet("{reportId}")]
        public async Task<ActionResult<Report>> ReadReportById([FromRoute] int reportId)
        {
            var report = await database.Reports.ReadReport(reportId);

            if (report == null)
            {
                return NotFound();
            }

            var authorization = await authorizationService.AllowTeamMembers(database, User, report.OrganizationName, report.ProjectId, report.TeamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return Report.FromPersistenceModel(report);
        }

        [HttpPatch("{reportId}")]
        public async Task<ActionResult> PatchReport([FromRoute] int reportId, [FromBody] PatchReportDto dto)
        {
            var report = await database.Reports.ReadReport(reportId);

            var authorization = await authorizationService.AllowTeamMembers(database, User, report.OrganizationName, report.ProjectId, report.TeamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

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
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return await reportCalculations.ListExistingReports(organizationName, projectId, teamId);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/available")]
        [HttpGet]
        public async Task<ActionResult<List<AvailableReport>>> ReadAllAvailableReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)  
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return await reportCalculations.ListAvailableReports(organizationName, projectId, teamId);
        }

        [Route("{reportId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteReport([FromRoute] int reportId)
        {
            var report = await database.Reports.ReadReport(reportId);

            var authorization = await authorizationService.AllowTeamMembers(database, User, report.OrganizationName, report.ProjectId, report.TeamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            await database.Reports.DeleteReport(reportId);

            return Ok();
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/timespan-sprints")]
        [HttpGet]
        public async Task<ActionResult<List<TimespanAdjustedSprint>>> ReadTimespanSprints(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return await commonCalculations.ReadAdjustedSprints(organizationName, projectId, teamId, start, end);
        }
    }
}
