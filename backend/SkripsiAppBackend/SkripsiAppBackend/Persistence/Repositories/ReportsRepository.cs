using Dapper;
using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Persistence.Repositories.Common;

namespace SkripsiAppBackend.Persistence.Repositories
{
    public class ReportsRepository : RepositoryBase
    {
        public ReportsRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task CreateReport(TrackedTeamsRepository.TrackedTeamKey teamKey, DateTime startDate, DateTime endDate, int expenditure)
        {
            var sql = @"
INSERT INTO reports (
    organization_name,
    project_id,
    team_id,
    start_date,
    end_date,
    expenditure
) VALUES (
    @OrganizationName,
    @ProjectId,
    @TeamId,
    @StartDate,
    @EndDate,
    @Expenditure
);";

            using var connection = GetConnection();

            var args = new
            {
                teamKey.OrganizationName,
                teamKey.ProjectId,
                teamKey.TeamId,
                StartDate = startDate,
                EndDate = endDate,
                Expenditure = expenditure
            };

            await connection.ExecuteAsync(sql, args);
        }

        public async Task UpdateExpenditure(int id, int expenditure)
        {
            var sql = @"UPDATE reports SET expenditure = @Expenditure WHERE id = @Id;";

            var args = new
            {
                Id = id,
                Expenditure = expenditure
            };

            using var connection = GetConnection();

            await connection.ExecuteAsync(sql, args);
        }

        public async Task<List<Report>> ReadTeamReports(TrackedTeamsRepository.TrackedTeamKey teamKey)
        {
            var sql = @"
SELECT * FROM reports WHERE 
    organization_name = @OrganizationName AND 
    project_id = @ProjectId AND 
    team_id = @TeamId;";

            using var connection = GetConnection();

            return (await connection.QueryAsync(sql, teamKey)).ToList().MapTo<Report>();
        }

        public async Task<Report> ReadReport(int id)
        {
            var sql = @"SELECT * FROM reports WHERE id = @Id";

            using var connection = GetConnection();

            var args = new { Id = id };

            return ModelMapper.MapTo<Report>(await connection.QuerySingleOrDefaultAsync(sql, args));
        }

        public async Task DeleteReport(int id)
        {
            var sql = @"DELETE FROM reports WHERE id = @Id;";

            using var connection = GetConnection();

            var args = new
            {
                Id = id
            };

            await connection.ExecuteAsync(sql, args);
        } 
    }
}
