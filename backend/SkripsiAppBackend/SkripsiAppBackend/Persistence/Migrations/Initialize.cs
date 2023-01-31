using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(1)]
    public class Initialize : Migration
    {
        public override void Up()
        {
            Create.Table("tracked_teams")
                .WithColumn("organization_name").AsString()
                .WithColumn("project_id").AsString()
                .WithColumn("team_id").AsString()
                .WithColumn("deleted").AsBoolean().WithDefaultValue(false);
               
            Create.PrimaryKey("pk_tracked_team")
                .OnTable("tracked_teams")
                .Columns("organization_name", "project_id", "team_id");
        }

        public override void Down()
        {
            Delete.Table("tracked_teams");
        }
    }
}
