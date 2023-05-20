using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkripsiAppBackend.Calculations;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Authorization;
using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Persistence.Repositories;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.DateTimeService;
using SkripsiAppBackend.Services.ObjectCachingService;
using SkripsiAppBackend.UseCases;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using static SkripsiAppBackend.Calculations.CommonCalculations;
using static SkripsiAppBackend.Calculations.TeamEvmCalculations;
using static SkripsiAppBackend.Calculations.TimeSeriesCalculations;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/teams")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly IAzureDevopsService azureDevopsService;
        private readonly Database database;
        private readonly IAuthorizationService authorizationService;
        private readonly IDateTimeService dateTimeService;
        private readonly TeamEvmCalculations evm;
        private readonly CommonCalculations common;
        private readonly MiscellaneousCalculations misc;
        private readonly TimeSeriesCalculations timeSeries;
        private readonly Configuration configuration;

        public TeamsController(
            IAzureDevopsService azureDevopsService, 
            Database database,
            IAuthorizationService authorizationService,
            IDateTimeService dateTimeService,
            TeamEvmCalculations evm,
            CommonCalculations common,
            MiscellaneousCalculations misc,
            TimeSeriesCalculations timeSeries,
            Configuration configuration)
        {
            this.azureDevopsService = azureDevopsService;
            this.database = database;
            this.authorizationService = authorizationService;
            this.dateTimeService = dateTimeService;
            this.evm = evm;
            this.common = common;
            this.misc = misc;
            this.timeSeries = timeSeries;
            this.configuration = configuration;
        }

        public struct Team
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public DateTime? Deadline { get; set; }
            public int? CostPerEffort { get; set; }
            public string EacFormula { get; set; }
            public string EtcFormula { get; set; }
            public bool Archived { get; set; }
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

            public Team AddModelDetails(Persistence.Models.TrackedTeam team)
            {
                Deadline = team.Deadline;
                CostPerEffort = team.CostPerEffort;
                EacFormula = team.EacFormula;
                EtcFormula = team.EtcFormula;

                return this;
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

            var teamsTask = existingTeams.Select(async team => await GetTeam(team.Item1, team.Item2, team.Item3)).ToList();

            var teams = await Task.WhenAll(teamsTask);

            return teams.ToList();
        }

        public struct TeamDetails
        {
            public Team Team { get; set; }
        }

        public struct SpiMetric
        {
            public double SchedulePerformanceIndex { get; set; }
        }

        public struct CpiMetric
        {
            public double CostPerformanceIndex { get; set; }
        }

        public struct FinanceStatus
        {
            public long ActualCost { get; set; }
            public long RemainingBudget { get; set; }
            public long BudgetAtCompletion { get; set; }
            public long CostPerEffort { get; set; }
            public long EstimateAtCompletion { get; set; }
            public long EstimateToCompletion { get; set; }
        }

        public struct TeamTimeline
        {
            public DateTime StartDate { get; set; }
            public DateTime Deadline { get; set; }
            public DateTime Now { get; set; }
            public DateTime EstimatedCompletionDate { get; set; }
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/spi")]
        public async Task<ActionResult<SpiMetric>> ReadTeamSpiMetric(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var spi = await evm.CalculateSchedulePerformanceIndex(organizationName, projectId, teamId, dateTimeService.GetNow());

            return new SpiMetric()
            {
                SchedulePerformanceIndex = spi
            };
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/cpi")]
        public async Task<ActionResult<CpiMetric>> ReadTeamCpiMetric(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var cpi = await evm.CalculateCostPerformanceIndex(organizationName, projectId, teamId, dateTimeService.GetNow());

            return new CpiMetric()
            {
                CostPerformanceIndex = cpi
            };
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/finances")]
        public async Task<ActionResult<FinanceStatus>> ReadTeamFinances(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);
            var team = await database.TrackedTeams.ReadByKey(teamKey);

            var remainingBudget = misc.CalculateRemainingBudget(organizationName, projectId, teamId, dateTimeService.GetNow());
            var actualCost = evm.CalculateActualCost(organizationName, projectId, teamId, dateTimeService.GetNow());
            var budgetAtCompletion = evm.CalculateBudgetAtCompletion(organizationName, projectId, teamId);

            var estimateAtCompletion = evm.CalculateEstimateAtCompletion(
                organizationName,
                projectId,
                teamId,
                dateTimeService.GetNow(),
                FormulaHelpers.FromString<EstimateAtCompletionFormulas>(team.EacFormula)
            );
            var estimateToCompletion = evm.CalculateEstimateToCompletion(
                organizationName,
                projectId,
                teamId,
                dateTimeService.GetNow(),
                FormulaHelpers.FromString<EstimateAtCompletionFormulas>(team.EacFormula)
            );

            await Task.WhenAll(remainingBudget, actualCost, budgetAtCompletion, estimateAtCompletion, estimateToCompletion);

            if (!team.CostPerEffort.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            return new FinanceStatus()
            {
                ActualCost = actualCost.Result,
                RemainingBudget = remainingBudget.Result,
                BudgetAtCompletion = budgetAtCompletion.Result,
                CostPerEffort = (long)team.CostPerEffort,
                EstimateAtCompletion = estimateAtCompletion.Result,
                EstimateToCompletion = estimateToCompletion.Result
            };
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/timeline")]
        public async Task<ActionResult<TeamTimeline>> ReadTeamTimeline(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);
            var team = await database.TrackedTeams.ReadByKey(teamKey);

            if (!team.Deadline.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var deadline = (DateTime)team.Deadline;
            var estimatedEndDate = misc.CalculateEstimatedCompletionDate(organizationName, projectId, teamId, dateTimeService.GetNow());

            await Task.WhenAll(startDate, estimatedEndDate);

            return new TeamTimeline()
            {
                StartDate = startDate.Result,
                Deadline = deadline,
                Now = dateTimeService.GetNow(),
                EstimatedCompletionDate = estimatedEndDate.Result,
            };
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/cpi-chart")]
        public async Task<ActionResult<List<CpiChartItem>>> ReadCpiChart(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var data = await timeSeries.CalculateCpiChart(organizationName, projectId, teamId);

            return data;
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/burndown-chart")]
        public async Task<ActionResult<BurndownChart>> ReadBurndownChart(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var data = await timeSeries.CalculateBurndownChart(organizationName, projectId, teamId);

            return data;
        }

        [HttpGet("{organizationName}/{projectId}/{teamId}/metrics/velocity-chart")]
        public async Task<ActionResult<List<VelocityChartItem>>> ReadVelocityChart(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            var data = await timeSeries.CalculateVelocityChart(organizationName, projectId, teamId);

            return data;
        }
        
        [HttpGet("{organizationName}/{projectId}/{teamId}")]
        public async Task<ActionResult<TeamDetails>> ReadTeamDetailsById(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            return new TeamDetails()
            {
                Team = await GetTeam(organizationName, projectId, teamId)
            };

            async Task<List<SprintWorkItems>> GetLatestSprints(List<IAzureDevopsService.Sprint> sprints, int count = 3)
            {
                var latestCompletedSprints = sprints
                    .FindAll(sprint => 
                        sprint.TimeFrame == IAzureDevopsService.SprintTimeFrame.Current ||
                        sprint.TimeFrame == IAzureDevopsService.SprintTimeFrame.Past)
                    .OrderBy(sprint => sprint.EndDate)
                    .Take(count);

                var sprintWorkItems = new List<SprintWorkItems>();
                var fetchTasks = latestCompletedSprints.Select(async (sprint) =>
                {
                    var workItems = await azureDevopsService.ReadSprintWorkItems(
                        organizationName,
                        projectId,
                        teamId,
                        sprint.Id);

                    sprintWorkItems.Add(new SprintWorkItems()
                    {
                        Sprint = sprint,
                        WorkItems = workItems
                    });
                });

                await Task.WhenAll(fetchTasks);

                return sprintWorkItems;
            }
        }
        private async Task<Team> GetTeam(string organizationName, string projectId, string teamId)
        {
            var trackedTeamTask = database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);
            var projectTask = azureDevopsService.ReadProject(organizationName, projectId);
            var teamTask = azureDevopsService.ReadTeam(organizationName, projectId, teamId);

            await Task.WhenAll(projectTask, teamTask, trackedTeamTask);

            return new Team()
            {
                Id = teamId,
                Name = teamTask.Result.Name,
                Project = projectTask.Result,
                Deadline = trackedTeamTask.Result.Deadline,
                CostPerEffort = trackedTeamTask.Result.CostPerEffort,
                EacFormula = trackedTeamTask.Result.EacFormula,
                EtcFormula = trackedTeamTask.Result.EtcFormula,
                Archived = trackedTeamTask.Result.Archived,
                Organization = new IAzureDevopsService.Organization()
                {
                    Name = organizationName
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
        [Authorize(Policy = AuthorizationPolicies.AllowAuthenticated)]
        public async Task<ActionResult> TrackTeam([FromBody] TrackTeamDto dto)
        {
            await database.TrackedTeams.CreateTrackedTeam(dto.OrganizationName, dto.ProjectId, dto.TeamId);
            
            return Ok();
        }

        [HttpDelete("{organizationName}/{projectId}/{teamId}")]
        public async Task<ActionResult> UntrackTeam(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId)
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            await database.TrackedTeams.UntrackTeam(organizationName, projectId, teamId);

            return Ok();
        }

        public struct UpdateTeamDto
        {
            public DateTime? Deadline { get; set; }
            public int? CostPerEffort { get; set; }
            public string? EacFormula { get; set; }
            public bool? Archived { get; set; }
        }

        [HttpPatch("{organizationName}/{projectId}/{teamId}")]
        public async Task<ActionResult> UpdateTeam(
            [FromRoute] string organizationName,
            [FromRoute] string projectId,
            [FromRoute] string teamId,
            [FromBody] UpdateTeamDto dto
        )
        {
            var authorization = await authorizationService.AllowTeamMembers(database, User, organizationName, projectId, teamId);
            if (!authorization.Succeeded)
            {
                return Unauthorized();
            }

            await database.TrackedTeams.UpdateTeam(organizationName,
                                                   projectId,
                                                   teamId,
                                                   dto.Deadline,
                                                   dto.CostPerEffort,
                                                   dto.EacFormula,
                                                   dto.Archived);

            return Ok();
        }
    }
}
