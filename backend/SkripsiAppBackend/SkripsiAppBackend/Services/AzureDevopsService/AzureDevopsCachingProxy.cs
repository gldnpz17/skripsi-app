using Microsoft.Extensions.ObjectPool;
using SkripsiAppBackend.Services.ObjectCachingService;
using SkripsiAppBackend.Services.UniversalCachingService;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public class AzureDevopsCachingProxy : IAzureDevopsService
    {
        private readonly IAzureDevopsService service;
        private readonly InMemoryUniversalCachingService cache;

        public AzureDevopsCachingProxy(
            IAzureDevopsService service,
            InMemoryUniversalCachingService cache)
        {
            this.service = service;
            this.cache = cache;
        }

        public bool HasActiveProfile => service.HasActiveProfile;

        public Task<List<IAzureDevopsService.Organization>> ReadAllOrganizations()
        {
            return service.ReadAllOrganizations();
        }

        public Task<List<IAzureDevopsService.WorkItem>> ReadBacklogWorkItems(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workItems", "backlog"),
                () => service.ReadBacklogWorkItems(organizationName, projectId, teamId),
                new List<Key> { new Key(organizationName, projectId, teamId, "workItems") }
            );
        }

        public Task<IAzureDevopsService.Project> ReadProject(string organizationName, string projectId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId),
                () => service.ReadProject(organizationName, projectId)
            );
        }

        public Task<List<IAzureDevopsService.Project>> ReadProjectsByOrganization(string organizationName)
        {
            return cache.UseCache(
                new Key(organizationName, "projects"),
                () => service.ReadProjectsByOrganization(organizationName)
            );
        }

        public IAzureDevopsService.Profile ReadSelfProfile()
        {
            return service.ReadSelfProfile();
        }

        public Task<List<IAzureDevopsService.WorkItem>> ReadSprintWorkItems(string organizationName, string projectId, string teamId, string sprintId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workItems", "sprints", sprintId),
                () => service.ReadSprintWorkItems(organizationName, projectId, teamId, sprintId),
                new List<Key> { new Key(organizationName, projectId, teamId, "workItems") }
            );
        }

        public Task<IAzureDevopsService.Team> ReadTeam(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId),
                () => service.ReadTeam(organizationName, projectId, teamId)
            );
        }

        public Task<List<IAzureDevopsService.Team>> ReadTeamsByProject(string organizationName, string projectId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, "teams"),
                () => service.ReadTeamsByProject(organizationName, projectId)
            );
        }

        public Task<List<IAzureDevopsService.Sprint>> ReadTeamSprints(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "sprints"),
                () => service.ReadTeamSprints(organizationName, projectId, teamId)
            );
        }

        public Task<List<DayOfWeek>> ReadTeamWorkDays(string organizationName, string projectId, string teamId)
        {
            return cache.UseCache(
                new Key(organizationName, projectId, teamId, "workDays"),
                () => service.ReadTeamWorkDays(organizationName, projectId, teamId)
            );
        }
    }
}
