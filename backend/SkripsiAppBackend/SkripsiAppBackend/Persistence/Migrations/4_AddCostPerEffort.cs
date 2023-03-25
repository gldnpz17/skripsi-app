using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(4)]
    public class AddCostPerEffort : Migration
    {
        public override void Up()
        {
            Alter.Table("tracked_teams")
                .AddColumn("cost_per_effort").AsInt32().Nullable();
        }

        public override void Down()
        {
            Delete.Column("cost_per_effort").FromTable("tracked_teams");
        }
    }
}
