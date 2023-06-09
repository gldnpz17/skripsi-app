using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Persistence.Repositories.TrackedTeams;
using SkripsiAppBackend.Services.UniversalCachingService;

namespace SkripsiAppBackend.Persistence.Repositories.Reports
{
    public class ReportsCachingProxy : IReportsRepository
    {
        private readonly IReportsRepository repository;
        private readonly InMemoryUniversalCachingService cache;

        public ReportsCachingProxy(
            IReportsRepository repository,
            InMemoryUniversalCachingService cache)
        {
            this.repository = repository;
            this.cache = cache;
        }

        public Task CreateReport(TrackedTeamsRepository.TrackedTeamKey teamKey, DateTime startDate, DateTime endDate, int expenditure)
        {
            cache.Invalidate(new Key("reports-cache"));
            return repository.CreateReport(teamKey, startDate, endDate, expenditure);
        }

        public Task DeleteReport(int id)
        {
            cache.Invalidate(new Key("reports-cache"));
            return repository.DeleteReport(id);
        }

        public Task<Report> ReadReport(int id)
        {
            return cache.UseCache(
                new Key("reports-cache", "report", id),
                () => repository.ReadReport(id),
                new List<Key> { new Key("reports-cache") }
            );
        }

        public Task<List<Report>> ReadTeamReports(TrackedTeamsRepository.TrackedTeamKey teamKey)
        {
            return cache.UseCache(
                new Key("reports-cache", teamKey.OrganizationName, teamKey.ProjectId, teamKey.TeamId, "reports"),
                () => repository.ReadTeamReports(teamKey),
                new List<Key> { new Key("reports-cache") }
            );
        }

        public Task UpdateExpenditure(int id, int expenditure)
        {
            cache.Invalidate(new Key("reports-cache"));
            return repository.UpdateExpenditure(id, expenditure);
        }
    }
}
