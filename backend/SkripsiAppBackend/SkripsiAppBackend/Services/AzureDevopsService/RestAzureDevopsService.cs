using Flurl.Http;
using Newtonsoft.Json;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Common.Deserialization;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Policy;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public class RestAzureDevopsService : IAzureDevopsService
    {
        private string refreshToken;
        private Profile profile = Profile.Empty;
        private readonly AccessTokenService accessTokenService;
        private const string AUTHORIZATION_HEADER_KEY = "Authorization";
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

        private struct Sprint
        {
            public string id { get; set; }
            public string name { get; set; }
            public SprintAttribute attributes { get; set; }
        }

        private struct SprintAttribute
        {
            public DateTime? startDate { get; set; }
            public DateTime? finishDate { get; set; }
            public string timeframe { get; set; }
        }

        private struct Project
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        private struct Team
        {
            public string id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
        }

        private struct WorkItemRelation
        {
            public struct Target
            {
                public string id { get; set; }
            }

            public Target target { get; set; }
        }

        private struct SprintWorkItems
        {
            public List<WorkItemRelation> workItemRelations;
        }

        private struct BacklogWorkItems
        {
            public List<WorkItemRelation> workItems { get; set; }
        }

        private struct TeamSettings
        {
            public List<string> workingDays { get; set; }
        }

        private struct WorkItemDetails
        {
            public struct Field
            {
                [JsonProperty(PropertyName = "System.State")]
                public string state { get; set; }

                [JsonProperty(PropertyName = "System.Title")]
                public string title { get; set; }

                [JsonProperty(PropertyName = "Microsoft.VSTS.Common.Priority")]
                public double priority { get; set; }

                [JsonProperty(PropertyName = "Microsoft.VSTS.Common.BusinessValue")]
                public double businessValue { get; set; }

                [JsonProperty(PropertyName = "Microsoft.VSTS.Scheduling.Effort")]
                public double effort { get; set; }
            }

            public string id { get; set; }

            public Field fields { get; set; }
        }

        public async Task<List<IAzureDevopsService.Organization>> ReadAllOrganizations()
        {
            var response = await $"https://app.vssps.visualstudio.com/_apis/accounts?memberId={profile.PublicAlias}&api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<ListResponse<Organization>>();

            return response.value
                .Select(organization => new IAzureDevopsService.Organization()
                {
                    Name = organization.accountName,
                })
                .ToList();
        }

        public async Task<List<IAzureDevopsService.Project>> ReadProjectsByOrganization(string organizationName)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects?api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
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
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<Project>();

            return new IAzureDevopsService.Project()
            {
                Id = response.id,
                Name = response.name
            };
        }

        public async Task<List<IAzureDevopsService.Team>> ReadTeamsByProject(string organizationName, string projectId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/projects/{projectId}/teams?api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
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
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<Team>();

            return new IAzureDevopsService.Team()
            {
                Id = response.id,
                Name = response.name,
                Description = response.description
            };
        }

        public async Task<List<IAzureDevopsService.Sprint>> ReadTeamSprints(string organizationName, string projectId, string teamId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/{projectId}/{teamId}/_apis/work/teamsettings/iterations?api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<ListResponse<Sprint>>();

            return response.value.Select(Map).ToList();

            static IAzureDevopsService.Sprint Map(Sprint sprint)
            {
                var timeframeDict = new Dictionary<string, SprintTimeFrame?>
                {
                    { "past", SprintTimeFrame.Past },
                    { "current", SprintTimeFrame.Current },
                    { "future", SprintTimeFrame.Future }
                };

                SprintTimeFrame? timeframe;
                timeframeDict.TryGetValue(sprint.attributes.timeframe, out timeframe);

                return new IAzureDevopsService.Sprint()
                {
                    Id = sprint.id,
                    Name = sprint.name,
                    StartDate = sprint.attributes.startDate,
                    EndDate = sprint.attributes.finishDate,
                    TimeFrame = timeframe ?? SprintTimeFrame.Unknown
                };
            }
        }

        public async Task<List<IAzureDevopsService.WorkItem>> ReadSprintWorkItems(string organizationName, string projectId, string teamId, string sprintId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/{projectId}/{teamId}/_apis/work/teamsettings/iterations/{sprintId}/workitems"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<SprintWorkItems>();

            return await GetWorkItemDetails(organizationName, response.workItemRelations);
        }

        public async Task<List<IAzureDevopsService.WorkItem>> ReadBacklogWorkItems(string organizationName, string projectId, string teamId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/{projectId}/{teamId}/_apis/work/backlogs/Microsoft.RequirementCategory/workItems?api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<BacklogWorkItems>();

            return await GetWorkItemDetails(organizationName, response.workItems);
        }

        private async Task<IAzureDevopsService.WorkItem> GetWorkItemDetails(string organizationName, WorkItemRelation workItemRelation)
        {
            var response = await $"https://dev.azure.com/{organizationName}/_apis/wit/workItems/{workItemRelation.target.id}"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<WorkItemDetails>();

            var stateDict = new Dictionary<string, IAzureDevopsService.WorkItemState?>
                {
                    { "New", WorkItemState.New },
                    { "Done", WorkItemState.Done }
                };

            return new IAzureDevopsService.WorkItem()
            {
                Id = response.id,
                State = stateDict[response.fields.state] ?? WorkItemState.Unknown,
                Title = response.fields.title,
                BusinessValue = response.fields.businessValue,
                Effort = response.fields.effort,
                Priority = response.fields.priority
            };
        }

        private async Task<List<IAzureDevopsService.WorkItem>> GetWorkItemDetails(
            string organizationName,
            List<WorkItemRelation> workItemRelations)
        {
            var detailRequests = workItemRelations.Select(async (workItemRelation) =>
            {
                return await GetWorkItemDetails(organizationName, workItemRelation);
            });

            await Task.WhenAll(detailRequests.ToArray());

            var detailResponses = new List<WorkItem>();
            foreach (var detailRequest in detailRequests)
            {
                detailResponses.Add(await detailRequest);
            }

            return detailResponses;
        }

        public async Task<List<DayOfWeek>> ReadTeamWorkDays(string organizationName, string projectId, string teamId)
        {
            var response = await $"https://dev.azure.com/{organizationName}/{projectId}/{teamId}/_apis/work/teamsettings?api-version=7.0"
                .WithHeader(AUTHORIZATION_HEADER_KEY, await GetAuthorizationHeader())
                .GetJsonAsync<TeamSettings>();

            var workDaysDict = new Dictionary<string, DayOfWeek>()
            {
                { "monday", DayOfWeek.Monday },
                { "tuesday", DayOfWeek.Tuesday },
                { "wednesday", DayOfWeek.Wednesday },
                { "thursday", DayOfWeek.Thursday },
                { "friday", DayOfWeek.Friday },
                { "saturday", DayOfWeek.Saturday },
                { "sunday", DayOfWeek.Sunday }
            };

            var workDays = new List<DayOfWeek>();
            foreach (var workDayString in response.workingDays)
            {
                if (workDaysDict.ContainsKey(workDayString))
                {
                    workDays.Add(workDaysDict[workDayString]);
                }
            }

            return workDays;
        }
    }
}
