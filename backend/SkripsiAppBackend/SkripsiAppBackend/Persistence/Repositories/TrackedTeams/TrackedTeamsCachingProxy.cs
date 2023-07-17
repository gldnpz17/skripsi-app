using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Services.UniversalCachingService;

namespace SkripsiAppBackend.Persistence.Repositories.TrackedTeams
{
    public class TrackedTeamsCachingProxy : ITrackedTeamsRepository
    {
        private readonly ITrackedTeamsRepository repository;
        private readonly InMemoryUniversalCachingService cache;

        public TrackedTeamsCachingProxy(
            ITrackedTeamsRepository repository,
            InMemoryUniversalCachingService cache)
        {
            this.repository = repository;
            this.cache = cache;
        }

        public Task CreateTrackedTeam(string organizationName, string projectId, string teamId)
        {
            cache.Invalidate(new Key("teams-cache"));
            return repository.CreateTrackedTeam(organizationName, projectId, teamId);
        }

        public Task<TrackedTeam> ReadByKey(string organizationName, string projectId, string teamId)
        {
            return ReadByKey(new TrackedTeamsRepository.TrackedTeamKey(organizationName, projectId, teamId));
        }

        public Task<TrackedTeam> ReadByKey(TrackedTeamsRepository.TrackedTeamKey key)
        {
            return cache.UseCache(
                new Key("teams-cache", "team", key.OrganizationName, key.ProjectId, key.TeamId),
                () => repository.ReadByKey(key),
                new List<Key> { new Key("teams-cache") }
            );
        }

        public Task<List<TrackedTeam>> ReadByKeys(List<TrackedTeamsRepository.TrackedTeamKey> keys)
        {
            return cache.UseCache(
                new Key("teams-cache", "teams", string.Join(";", keys)),
                () => repository.ReadByKeys(keys),
                new List<Key> { new Key("teams-cache") }
            );
        }

        public Task UntrackTeam(string organizationName, string projectId, string teamId)
        {
            cache.Invalidate(new Key("teams-cache"));
            return repository.UntrackTeam(organizationName, projectId, teamId);
        }

        public Task UpdateTeam(string organizationName, string projectId, string teamId, DateTime? deadline, int? costPerEffort, string? eacFormula, bool? archived)
        {
            cache.Invalidate(new Key("teams-cache"));
            return repository.UpdateTeam(organizationName, projectId, teamId, deadline, costPerEffort, eacFormula, archived);
        }
    }
}
