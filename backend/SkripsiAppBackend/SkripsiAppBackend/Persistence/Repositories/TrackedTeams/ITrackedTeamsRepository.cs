using SkripsiAppBackend.Persistence.Models;

namespace SkripsiAppBackend.Persistence.Repositories.TrackedTeams
{
    public interface ITrackedTeamsRepository
    {
        Task CreateTrackedTeam(string organizationName, string projectId, string teamId);
        Task<TrackedTeam> ReadByKey(string organizationName, string projectId, string teamId);
        Task<TrackedTeam> ReadByKey(TrackedTeamsRepository.TrackedTeamKey key);
        Task<List<TrackedTeam>> ReadByKeys(List<TrackedTeamsRepository.TrackedTeamKey> keys);
        Task UntrackTeam(string organizationName, string projectId, string teamId);
        Task UpdateTeam(string organizationName, string projectId, string teamId, DateTime? deadline, int? costPerEffort, string? eacFormula, bool? archived);
    }
}