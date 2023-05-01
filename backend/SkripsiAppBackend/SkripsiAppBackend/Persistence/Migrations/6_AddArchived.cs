using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(6)]
    public class AddArchived : Migration
    {
        public override void Up()
        {
            Alter.Table("tracked_teams")
                .AddColumn("archived").AsBoolean().NotNullable().WithDefaultValue(false);

            Delete.Column("deleted").FromTable("tracked_teams");
            Delete.Column("deleted").FromTable("reports");
        }

        public override void Down()
        {
            Delete.Column("archived").FromTable("tracked_teams");

            Alter.Table("tracked_teams")
                .AddColumn("deleted").AsBoolean().NotNullable().WithDefaultValue(false);
            Alter.Table("reports")
                .AddColumn("deleted").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
