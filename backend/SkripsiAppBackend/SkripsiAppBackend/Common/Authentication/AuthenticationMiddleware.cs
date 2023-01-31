using Jose;
using SkripsiAppBackend.Controllers;
using SkripsiAppBackend.DomainModel;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.ObjectCachingService;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SkripsiAppBackend.Common.Authentication
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate next;

        public AuthenticationMiddleware(RequestDelegate requestDelegate)
        {
            next = requestDelegate;
        }

        public struct ProfileTeam
        {
            public IAzureDevopsService.Team Team { get; set; }
            public IAzureDevopsService.Project Project { get; set; }
            public IAzureDevopsService.Organization Organization { get; set; }
            public bool Equals(TrackedTeam team)
            {
                return (
                    team.OrganizationName == Organization.Name &&
                    team.ProjectId == Project.Id &&
                    team.TeamId == Team.Id
                );
            }
        }

        public async Task<List<ProfileTeam>> FetchProfileTeams(
            IObjectCachingService<List<ProfileTeam>> teamsCachingService,
            IAzureDevopsService azureDevopsService)
        {
            return await teamsCachingService.GetCache(azureDevopsService.ReadSelfProfile().ProfileId, async () =>
            {
                var organizations = await azureDevopsService.ReadAllOrganizations();
                var teams =
                    (await Task.WhenAll(
                        organizations.Select(async (organization) => await FetchOrganizationTeams(organization))
                    ))
                    .SelectMany(teams => teams)
                    .ToList();

                return teams;
            });

            async Task<List<ProfileTeam>> FetchOrganizationTeams(IAzureDevopsService.Organization organization)
            {
                var projects = await azureDevopsService.ReadProjectsByOrganization(organization.Name);

                var teams =
                    (await Task.WhenAll(
                        projects.Select(async (project) => await FetchProjectTeams(organization, project))
                    ))
                    .SelectMany(teams => teams)
                    .ToList();

                return teams;
            }

            async Task<List<ProfileTeam>> FetchProjectTeams(IAzureDevopsService.Organization organization, IAzureDevopsService.Project project)
            {
                var teams = await azureDevopsService.ReadTeamsByProject(organization.Name, project.Id);

                return teams
                    .Select(team => new ProfileTeam()
                    {
                        Team = team,
                        Organization = organization,
                        Project = project
                    })
                    .ToList();
            }
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IAzureDevopsService azureDevopsService,
            IObjectCachingService<List<ProfileTeam>> teamsCachingService)
        {
            if (azureDevopsService.HasActiveProfile)
            {
                var teams = await FetchProfileTeams(teamsCachingService, azureDevopsService);

                var claims = new List<Claim>()
                {
                    new Claim("teams", JsonSerializer.Serialize(teams))
                };

                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            }

            await next(httpContext);
        }
    }

    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAzureDevopsAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
