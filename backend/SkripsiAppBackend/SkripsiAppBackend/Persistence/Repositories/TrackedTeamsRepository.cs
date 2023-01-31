using Dapper;
using SkripsiAppBackend.Persistence.Models;
using SkripsiAppBackend.Persistence.Repositories.Common;

namespace SkripsiAppBackend.Persistence.Repositories
{
    public class TrackedTeamsRepository : RepositoryBase
    {
        public TrackedTeamsRepository(string connectionString) : base(connectionString)
        {
        }

        public struct TrackedTeamKey
        {
            public string OrganizationName { get; set; }
            public string ProjectId { get; set; }
            public string TeamId { get; set; }
        }

        public async Task<List<TrackedTeam>> ReadByKeys(List<TrackedTeamKey> keys)
        {
            // I'm somewhat annoyed. EF Core doesn't seem to support temporary tables very well and dapper
            // doesn't support bulk inserts. This solution is suboptimal but it will do for now.

            var createTempTableSql = @"CREATE TEMPORARY TABLE IF NOT EXISTS primary_keys(organization_name TEXT, project_id TEXT, team_id TEXT);";
            var insertKeyRowSql = @"INSERT INTO primary_keys(organization_name, project_id, team_id) VALUES (@OrganizationName, @ProjectId, @TeamId);";
            var querySql = @"
SELECT tracked_teams.* FROM tracked_teams
    JOIN primary_keys ON (
        tracked_teams.organization_name = primary_keys.organization_name AND
        tracked_teams.project_id = primary_keys.project_id AND
        tracked_teams.team_id = primary_keys.team_id
    )
    WHERE deleted = false;
";

            using var connection = GetConnection();

            await connection.OpenAsync();
            await connection.ExecuteAsync(createTempTableSql);
            await Task.WhenAll(
                keys.Select(
                    async (key) => await connection.ExecuteAsync(insertKeyRowSql, key)
                )
            );
            return (await connection.QueryAsync(querySql)).ToList().MapTo<TrackedTeam>();
        }

        public async Task<TrackedTeam> ReadByKey(string organizationName, string projectId, string teamId)
        {
            var sql = @"
SELECT * FROM tracked_teams WHERE 
    organization_name = @OrganizationName AND
    project_id = @ProjectId AND
    teamId = @TeamId AND
    deleted = false;
";

            using var connection = GetConnection();

            var args = new {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            return ModelMapper.MapTo<TrackedTeam>(await connection.QuerySingleOrDefaultAsync(sql, args));
        }

        public async Task CreateTrackedTeam(string organizationName, string projectId, string teamId)
        {
            var sql = @"
INSERT INTO tracked_teams (organization_name, project_id, team_id)
VALUES (@OrganizationName, @ProjectId, @TeamId)
ON CONFLICT ON CONSTRAINT pk_tracked_team DO UPDATE
    SET deleted = false;
";

            using var connection = GetConnection();

            var args = new
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            await connection.ExecuteAsync(sql, args);
        }

        public async Task UntrackTeam(string organizationName, string projectId, string teamId)
        {
            var sql = @"
UPDATE tracked_teams
SET deleted = true 
WHERE 
    organization_name = @OrganizationName AND
    project_id = @ProjectId AND
    teamId = @TeamId;
";

            using var connection = GetConnection();

            var args = new
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId
            };

            await connection.ExecuteAsync(sql, args);
        }
    }
}
