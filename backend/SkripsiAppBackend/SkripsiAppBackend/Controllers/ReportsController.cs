using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Common.Authorization;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.UseCases;
using System.Runtime.CompilerServices;
using static SkripsiAppBackend.UseCases.MetricCalculations;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : Controller
    {
        private readonly Database database;
        private readonly MetricCalculations metricCalculations;
        private readonly IAuthorizationService authorizationService;

        public ReportsController(
            Database database,
            MetricCalculations metricCalculations,
            IAuthorizationService authorizationService)
        {
            this.database = database;
            this.metricCalculations = metricCalculations;
            this.authorizationService = authorizationService;
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

            public MetricCalculations.Report ToUseCaseModel()
            {
                return new MetricCalculations.Report()
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
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            await metricCalculations.CreateReport(organizationName, projectId, teamId, dto.ToUseCaseModel());

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

            return await metricCalculations.ListExistingReports(organizationName, projectId, teamId);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/available")]
        [HttpGet]
        public async Task<ActionResult<List<MetricCalculations.AvailableReport>>> ReadAllAvailableReports(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)  
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return await metricCalculations.ListAvailableReports(organizationName, projectId, teamId);
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
        public async Task<ActionResult<List<MetricCalculations.ReportSprint>>> ReadTimespanSprints(
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

            return await metricCalculations.GetTimespanSprints(organizationName, projectId, teamId, start, end);
        }

        [Route("/api/teams/{organizationName}/{projectId}/{teamId}/reports/new-report-metrics")]
        [HttpGet]
        public async Task<ActionResult<MetricCalculations.ReportMetrics>> ReadNewReportMetrics(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] int expenditure)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return await metricCalculations.CalculateReportMetrics(
                organizationName,
                projectId,
                teamId, new MetricCalculations.Report()
                {
                    StartDate = start,
                    EndDate = end,
                    Expenditure = expenditure
                });
        }
    }
}
