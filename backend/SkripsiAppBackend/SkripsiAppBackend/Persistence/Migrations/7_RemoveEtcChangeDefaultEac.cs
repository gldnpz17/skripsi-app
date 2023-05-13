using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(7)]
    public class RemoveEtcChangeDefaultEac : Migration
    {
        public override void Up()
        {
            Alter.Table("tracked_teams")
                .AlterColumn("eac_formula").AsString().WithDefaultValue("Typical");

            Delete.Column("etc_formula").FromTable("tracked_teams");
        }

        public override void Down()
        {
            Alter.Table("tracked_teams")
                .AlterColumn("eac_formula").AsString().WithDefaultValue("Basic");

            Alter.Table("tracked_teams")
                .AddColumn("etc_formula").AsString().WithDefaultValue("Derived");
        }
    }
}
