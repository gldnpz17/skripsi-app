using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.ObjectCachingService;
using SkripsiAppBackend.UseCases;
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
        private readonly TeamUseCases teamUseCases;
        private readonly Configuration configuration;

        public TeamsController(
            IAzureDevopsService azureDevopsService, 
            Database database,
            IAuthorizationService authorizationService,
            TeamUseCases teamUseCases,
            Configuration configuration)
        {
            this.azureDevopsService = azureDevopsService;
            this.database = database;
            this.authorizationService = authorizationService;
            this.teamUseCases = teamUseCases;
            this.configuration = configuration;
        }

        public struct Team
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public DateTime? Deadline { get; set; }

            public int? CostPerEffort { get; set; }
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
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}")]
        public async Task<ActionResult<TeamDetails>> ReadTeamDetailsById(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var trackedTeam = await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);

            var authorizationResult = await authorizationService.AuthorizeAsync(User, trackedTeam, AuthorizationPolicies.AllowTeamMember);
            if (!authorizationResult.Succeeded)
            {
                return Unauthorized();
            }

            return new TeamDetails()
            {
                Team = await GetTeam()
            };

            async Task<Team> GetTeam()
            {
                var projectTask = azureDevopsService.ReadProject(organizationName, projectId);
                var teamTask = azureDevopsService.ReadTeam(organizationName, projectId, teamId);

                await Task.WhenAll(projectTask, teamTask);

                var project = await projectTask;
                var team = await teamTask;

                return new Team()
                {
                    Id = teamId,
                    Name = team.Name,
                    Project = project,
                    Deadline = trackedTeam.Deadline,
                    CostPerEffort = trackedTeam.CostPerEffort,
                    Organization = new IAzureDevopsService.Organization()
                    {
                        Name = organizationName
                    }
                };
            }

            async Task<List<TeamUseCases.SprintWorkItems>> GetLatestSprints(List<IAzureDevopsService.Sprint> sprints, int count = 3)
            {
                var latestCompletedSprints = sprints
                    .FindAll(sprint => 
                        sprint.TimeFrame == IAzureDevopsService.SprintTimeFrame.Current ||
                        sprint.TimeFrame == IAzureDevopsService.SprintTimeFrame.Past)
                    .OrderBy(sprint => sprint.EndDate)
                    .Take(count);

                var sprintWorkItems = new List<TeamUseCases.SprintWorkItems>();
                var fetchTasks = latestCompletedSprints.Select(async (sprint) =>
                {
                    var workItems = await azureDevopsService.ReadSprintWorkItems(
                        organizationName,
                        projectId,
                        teamId,
                        sprint.Id);

                    sprintWorkItems.Add(new TeamUseCases.SprintWorkItems()
                    {
                        Sprint = sprint,
                        WorkItems = workItems
                    });
                });

                await Task.WhenAll(fetchTasks);

                return sprintWorkItems;
            }
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

        public struct UpdateTeamDto
        {
            public DateTime? Deadline { get; set; }
            public int CostPerEffort { get; set; }
        }

        [HttpPatch("{organizationName}/{projectId}/{teamId}")]
        public async Task<ActionResult> UpdateTeam(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromBody] UpdateTeamDto dto
        )
        {
            await database.TrackedTeams.UpdateTeam(organizationName,
                                                   projectId,
                                                   teamId,
                                                   dto.Deadline,
                                                   dto.CostPerEffort);

            return Ok();
        }
    }
}
