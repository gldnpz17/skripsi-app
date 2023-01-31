using Microsoft.EntityFrameworkCore;
using SkripsiAppBackend.DomainModel;

namespace SkripsiAppBackend.Persistence
{
    public class ApplicationDatabase : DbContext
    {
        public DbSet<TrackedTeam> TrackedTeams { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Use a composite primary key.
            builder.Entity<TrackedTeam>().HasKey(t => new { t.TeamId, t.ProjectId, t.OrganizationName });
        }
    }

    public class InMemoryApplicationDatabase : ApplicationDatabase
    {
        private readonly string databaseName;

        public InMemoryApplicationDatabase(string databaseName)
        {
            this.databaseName = databaseName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseInMemoryDatabase(databaseName);
        }
    }

    public class SqliteApplicationDatabase : ApplicationDatabase
    {
        private readonly string databasePath;

        public SqliteApplicationDatabase(string databaseName = "app_database.db")
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            databasePath = Path.Join(path, databaseName);

            Console.WriteLine($"Using Sqlite database stored in {databasePath}.");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }

    public class PostgresqlApplicationDatabase : ApplicationDatabase
    {
        private readonly string connectionString;

        public PostgresqlApplicationDatabase(string connectionString)
        {
            this.connectionString = connectionString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}
