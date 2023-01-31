using Microsoft.Data.SqlClient;
using Npgsql;

namespace SkripsiAppBackend.Persistence.Repositories.Common
{
    public abstract class RepositoryBase
    {
        protected readonly string connectionString;

        public RepositoryBase(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
