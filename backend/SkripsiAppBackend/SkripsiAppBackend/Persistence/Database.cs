using FluentMigrator.Postgres;
using FluentMigrator;
using FluentMigrator.Runner;
using SkripsiAppBackend.Persistence.Migrations;
using System.Reflection;
using SkripsiAppBackend.Persistence.Repositories.Reports;
using SkripsiAppBackend.Persistence.Repositories.TrackedTeams;
using SkripsiAppBackend.Services.UniversalCachingService;

namespace SkripsiAppBackend.Persistence
{
    public class Database
    {
        private readonly string connectionString;

        public ITrackedTeamsRepository TrackedTeams { get; private set; }
        public IReportsRepository Reports { get; private set; }

        public Database(string connectionString)
        {
            this.connectionString = connectionString;

            TrackedTeams = new TrackedTeamsRepository(connectionString);
            Reports = new ReportsRepository(connectionString);
        }

        public Database AddCache(InMemoryUniversalCachingService cache)
        {
            TrackedTeams = new TrackedTeamsCachingProxy(TrackedTeams, cache);
            Reports = new ReportsCachingProxy(Reports, cache);
            return this;
        }

        public static void Migrate(string connectionString, int version = 100)
        {
            Console.WriteLine($"Attempting to migrate database to version {version}.");

            var serviceCollection = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(builder =>
                {
                    builder
                        .AddPostgres()
                        .WithGlobalConnectionString(connectionString)
                        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations();
                })
                .BuildServiceProvider(false);

            var runner = serviceCollection.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp(version);
        }
    }
}
