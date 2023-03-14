using FluentMigrator;
using FluentMigrator.Postgres;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(3)]
    public class AddReports : Migration
    {
        public override void Up()
        {
            Create.Table("reports")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("deleted").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("organization_name").AsString().NotNullable()
                .WithColumn("project_id").AsString().NotNullable()
                .WithColumn("team_id").AsString().NotNullable()
                .WithColumn("start_date").AsDateTime().NotNullable()
                .WithColumn("end_date").AsDateTime().NotNullable()
                .WithColumn("expenditure").AsInt64().NotNullable().WithDefaultValue(0);

            Create.ForeignKey("fk_report_tracked_team")
                .FromTable("reports")
                .ForeignColumns("organization_name", "project_id", "team_id")
                .ToTable("tracked_teams")
                .PrimaryColumns("organization_name", "project_id", "team_id");
        }

        public override void Down()
        {
            Delete.Table("reports");

            Delete.ForeignKey("fk_report_tracked_team");
        }
    }
}
