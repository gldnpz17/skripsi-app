using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Persistence.Repositories.TrackedTeams;

namespace SkripsiAppBackend.Persistence.Repositories.Reports
{
    public interface IReportsRepository
    {
        Task CreateReport(TrackedTeamsRepository.TrackedTeamKey teamKey, DateTime startDate, DateTime endDate, int expenditure);
        Task DeleteReport(int id);
        Task<Report> ReadReport(int id);
        Task<List<Report>> ReadTeamReports(TrackedTeamsRepository.TrackedTeamKey teamKey);
        Task UpdateExpenditure(int id, int expenditure);
    }
}