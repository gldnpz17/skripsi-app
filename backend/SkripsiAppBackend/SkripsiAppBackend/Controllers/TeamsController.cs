using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.DomainModel;
using SkripsiAppBackend.Persistence;
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
        private readonly ApplicationDatabase database;
        private readonly IAuthorizationService authorizationService;

        public TeamsController(
            IAzureDevopsService azureDevopsService, 
            ApplicationDatabase database,
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

            public bool Equals(DomainModel.TrackedTeam team)
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
            //var filterTuples = profileTeams.Select(team => new Tuple<string, string, string>(team.Organization.Name, team.Project.Name, team.Id)).ToList();

            var filterTuples = new List<(string organization, string project, string team)>
            {
                ("firdausbismasuryakusuma", "5d7be433-a372-4424-8628-784e53867899", "10938e39-440f-40db-a5f5-71e1003a2ec0"),
                ("firdausbismasuryakusuma", "5d7be433-a372-4424-8628-784e53867899", "a511772c-5315-41b5-993c-103eb4495c5c")
            };

            /*var filterTuples = new List<object>
            {
                new { OrganizationName = "firdausbismasuryakusuma", ProjectId = "5d7be433-a372-4424-8628-784e53867899", TeamId = "10938e39-440f-40db-a5f5-71e1003a2ec0" },
                new { OrganizationName = "firdausbismasuryakusuma", ProjectId = "5d7be433-a372-4424-8628-784e53867899", TeamId = "a511772c-5315-41b5-993c-103eb4495c5c" }
            };*/

            var existingTeamIds =
                (await database.TrackedTeams
                    .Where(team =>
                        filterTuples.Any(tuple => tuple.organization == team.OrganizationName && tuple.project == team.ProjectId && tuple.team == team.TeamId) &&
                        team.IsUntracked == false
                    )
                    .ToListAsync()
                )
                .Select(team => (team.OrganizationName, team.ProjectId, team.TeamId))
                .ToList();

            Console.WriteLine(
                database.TrackedTeams
                    .Where(team =>
                        filterTuples.Any(tuple => tuple.organization == team.OrganizationName && tuple.project == team.ProjectId && tuple.team == team.TeamId) &&
                        team.IsUntracked == false
                    )
                    .ToQueryString()
            );

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

            var at = await database.TrackedTeams.ToListAsync();

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
            var trackedTeam = await database.TrackedTeams
                .Where(team => team.OrganizationName == organizationName && team.ProjectId == projectId && team.TeamId == teamId)
                .FirstAsync();

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
            var existingTeam = await database.TrackedTeams
                .FirstOrDefaultAsync(team =>
                    team.TeamId == dto.TeamId &&
                    team.ProjectId == dto.ProjectId &&
                    team.OrganizationName == dto.OrganizationName
                );
            
            if (existingTeam != null)
            {
                existingTeam.IsUntracked = false;
                await database.SaveChangesAsync();
                return Ok();
            }

            var trackedTeam = new TrackedTeam()
            {
                TeamId = dto.TeamId,
                ProjectId = dto.ProjectId,
                OrganizationName = dto.OrganizationName,
            };

            await database.TrackedTeams.AddAsync(trackedTeam);
            await database.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete]
        public async Task<ActionResult> UntrackTeam([FromBody] TrackTeamDto dto)
        {
            var existingTeam = await database.TrackedTeams
                .FirstOrDefaultAsync(team =>
                    team.TeamId == dto.TeamId &&
                    team.ProjectId == dto.ProjectId &&
                    team.OrganizationName == dto.OrganizationName
                );

            if (existingTeam == null)
            {
                return NotFound();
            }

            existingTeam.IsUntracked = false;
            await database.SaveChangesAsync();
            return Ok();
        }
    }
}
