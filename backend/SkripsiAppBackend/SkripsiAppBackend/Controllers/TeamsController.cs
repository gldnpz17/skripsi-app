using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.ObjectCachingService;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/teams")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly IAzureDevopsService azureDevopsService;
        private readonly Database database;
        private readonly IAuthorizationService authorizationService;

        public TeamsController(
            IAzureDevopsService azureDevopsService, 
            Database database,
            IAuthorizationService authorizationService)
        {
            this.azureDevopsService = azureDevopsService;
            this.database = database;
            this.authorizationService = authorizationService;
        }

        public struct Team
        {
            public string Id { get; set; }
            public string Name { get; set; }

            public IAzureDevopsService.Organization Organization { get; set; }
            public IAzureDevopsService.Project Project { get; set; }

            public bool Equals(Persistence.Models.TrackedTeam team)
            {
                return (
                    team.OrganizationName == Organization.Name &&
                    team.ProjectId == Project.Id &&
                    team.TeamId == Id
                );
            }

            public static Team FromProfileTeam(AuthenticationMiddleware.ProfileTeam profileTeam)
            {
                return new Team()
                {
                    Id = profileTeam.Team.Id,
                    Name = profileTeam.Team.Name,
                    Organization = profileTeam.Organization,
                    Project = profileTeam.Project,
                };
            }
        }

        private async Task<List<(string, string, string)>> FetchExistingTeamIds(List<Team> profileTeams)
        {
            var keys = profileTeams
                .Select(team => new TrackedTeamsRepository.TrackedTeamKey()
                {
                    OrganizationName = team.Organization.Name,
                    ProjectId = team.Project.Id,
                    TeamId = team.Id
                })
                .ToList();

            var existingTeamIds =
                (await database.TrackedTeams
                    .ReadByKeys(keys)
                )
                .Select(team => (team.OrganizationName, team.ProjectId, team.TeamId))
                .ToList();

            return existingTeamIds;
        }

        [HttpGet("untracked")]
        public async Task<ActionResult<List<Team>>> ReadAllUntrackedTeams([FromQuery] string projectId)
        {
            var profileTeams = User.GetTeams().Select(profileTeam => Team.FromProfileTeam(profileTeam)).ToList();

            var existingTeams = await FetchExistingTeamIds(profileTeams);

            return profileTeams
                .Where(team => !existingTeams.Contains((team.Organization.Name, team.Project.Id, team.Id)))
                .Where(team => projectId == null || team.Project.Id == projectId)
                .ToList();
        }

        [HttpGet("tracked")]
        public async Task<ActionResult<List<Team>>> ReadAllTrackedTeams()
        {
            var profileTeams = User.GetTeams().Select(profileTeam => Team.FromProfileTeam(profileTeam)).ToList();

            var existingTeams = await FetchExistingTeamIds(profileTeams);

            return profileTeams
                .Where(team => existingTeams.Contains((team.Organization.Name, team.Project.Id, team.Id)))
                .ToList();
        }

        public struct TeamDetails
        {
            public Team Team { get; set; }
            // TODO: Add further details for the dashboard.
        }

        [HttpGet]
        public async Task<ActionResult<TeamDetails>> ReadTeamDetailsById(
            [FromQuery] string organizationName,
            [FromQuery] string projectId,
            [FromQuery] string teamId) 
        {
            var trackedTeam = await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);

            var authorizationResult = await authorizationService.AuthorizeAsync(User, trackedTeam, AuthorizationPolicies.AllowTeamMember);
            if (!authorizationResult.Succeeded) 
            {
                return Unauthorized();
            }

            var projectTask = azureDevopsService.ReadProject(organizationName, projectId);
            var teamTask = azureDevopsService.ReadTeam(organizationName, projectId, teamId);

            await Task.WhenAll(projectTask, teamTask);

            var project = await projectTask;
            var team = await teamTask;

            return new TeamDetails()
            {
                Team = new Team()
                {
                    Id = teamId,
                    Name = team.Name,
                    Project = project,
                    Organization = new IAzureDevopsService.Organization()
                    {
                        Name = organizationName
                    }
                }
            };
        }

        public struct TrackTeamDto
        {
            public string TeamId { get; set; }
            public string ProjectId { get; set; }
            public string OrganizationName { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult> TrackTeam([FromBody] TrackTeamDto dto)
        {
            await database.TrackedTeams.CreateTrackedTeam(dto.OrganizationName, dto.ProjectId, dto.TeamId);
            
            return Ok();
        }

        [HttpDelete]
        public async Task<ActionResult> UntrackTeam([FromBody] TrackTeamDto dto)
        {
            await database.TrackedTeams.UntrackTeam(dto.OrganizationName, dto.ProjectId, dto.TeamId);

            return Ok();
        }
    }
}
