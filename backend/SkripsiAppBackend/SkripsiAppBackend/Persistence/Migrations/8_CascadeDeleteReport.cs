using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(8)]
    public class CascadeDeleteReport : Migration
    {
        public override void Up()
        {
            Delete.ForeignKey("fk_report_tracked_team").OnTable("reports");

            Create.ForeignKey("fk_report_tracked_team")
                .FromTable("reports")
                .ForeignColumns("organization_name", "project_id", "team_id")
                .ToTable("tracked_teams")
                .PrimaryColumns("organization_name", "project_id", "team_id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        public override void Down()
        {
            Delete.ForeignKey("fk_report_tracked_team").OnTable("reports");

            Create.ForeignKey("fk_report_tracked_team")
                .FromTable("reports")
                .ForeignColumns("organization_name", "project_id", "team_id")
                .ToTable("tracked_teams")
                .PrimaryColumns("organization_name", "project_id", "team_id");
        }
    }
}
