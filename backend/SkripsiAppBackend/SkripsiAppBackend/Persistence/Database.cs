using FluentMigrator.Postgres;
using FluentMigrator;
using FluentMigrator.Runner;
using SkripsiAppBackend.Persistence.Migrations;
using System.Reflection;
using SkripsiAppBackend.Persistence.Repositories;

namespace SkripsiAppBackend.Persistence
{
    public class Database
    {
        private readonly string connectionString;
        private readonly int version;

        public TrackedTeamsRepository TrackedTeams { get; private set; }

        public Database(string connectionString, int version = 100)
        {
            this.connectionString = connectionString;
            this.version = version;
            TrackedTeams = new TrackedTeamsRepository(connectionString);
        }

        public void Migrate()
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
