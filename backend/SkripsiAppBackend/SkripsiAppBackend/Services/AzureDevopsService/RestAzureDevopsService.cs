using Flurl.Http;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Deserialization;
using System.Linq.Expressions;
using System.Security.Claims;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public class RestAzureDevopsService : IAzureDevopsService
    {
        private string refreshToken;
        private Profile profile = Profile.Empty;
        private readonly AccessTokenService accessTokenService;
        public RestAzureDevopsService(AccessTokenService accessTokenService)
        {
            this.accessTokenService = accessTokenService;
        }

        public void SetProfile(string profileId, string publicAlias, string displayName, string refreshToken, Guid sessionId)
        {
            this.refreshToken = refreshToken;
            profile = new Profile()
            {
                ProfileId = profileId,
                sessionId = sessionId,
                DisplayName = displayName,
                PublicAlias = publicAlias
            };
        }

        public struct ListResponse<TItem>
        {
            public int count { get; set; }
            public List<TItem> value { get; set; }
        }

        private async Task<string> GetAuthorizationHeader()
        {
            return await accessTokenService.GetToken(refreshToken);
        }

        public bool HasActiveProfile
        {
            get
            {
                return !profile.Equals(Profile.Empty);
            }
        }

        public Profile ReadSelfProfile()
        {
            return profile;
        }

        private struct Organization
        {
            public string accountId { get; set; }
            public string accountName { get; set; }
        }

        public async Task<List<IAzureDevopsService.Organization>> ReadAllOrganizations()
        {
            var response = await $"https://app.vssps.visualstudio.com/_apis/accounts?memberId={profile.PublicAlias}&api-version=7.0"
                .WithHeader("Authorization", await GetAuthorizationHeader())
                .GetJsonAsync<ListResponse<Organization>>();

            return response.value
                .Select(organization => new IAzureDevopsService.Organization()
                {
                    Name = organization.accountName,
                })
                .ToList();
        }
        public struct Project
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public async Task<List<IAzureDevopsService.Project>> ReadProjectsByOrganization(string organizationName)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects?api-version=7.0"
                .WithHeader("Authorization", await GetAuthorizationHeader())
                .GetJsonAsync<ListResponse<Project>>();

            return response.value
                .Select(project => new IAzureDevopsService.Project()
                {
                    Id = project.id,
                    Name = project.name
                })
                .ToList();
        }

        public async Task<IAzureDevopsService.Project> ReadProject(string organizationName, string projectId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects/{projectId}?api-version=7.0"
                .WithHeader("Authorization", await GetAuthorizationHeader())
                .GetJsonAsync<Project>();

            return new IAzureDevopsService.Project()
            {
                Id = response.id,
                Name = response.name
            };
        }

        struct Team
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
        }

        public async Task<List<IAzureDevopsService.Team>> ReadTeamsByProject(string organizationName, string projectId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects/{projectId}/teams?api-version=7.0"
                .WithHeader("Authorization", await GetAuthorizationHeader())
                .GetJsonAsync<ListResponse<Team>>();

            return response.value
                .Select(team => new IAzureDevopsService.Team()
                {
                    Id = team.id,
                    Name = team.name,
                    Description = team.description
                })
                .ToList();
        }

        public async Task<IAzureDevopsService.Team> ReadTeam(string organizationName, string projectId, string teamId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects/{projectId}/teams/{teamId}?api-version=7.0"
                .WithHeader("Authorization", await GetAuthorizationHeader())
                .GetJsonAsync<Team>();

            return new IAzureDevopsService.Team()
            {
                Id = response.id,
                Name = response.name,
                Description = response.description
            };
        }
    }
}
