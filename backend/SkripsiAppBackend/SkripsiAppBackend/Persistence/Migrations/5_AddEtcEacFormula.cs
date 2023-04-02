using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(5)]
    public class AddEtcEacFormula : Migration
    {
        public override void Up()
        {
            Alter.Table("tracked_teams")
                .AddColumn("eac_formula").AsString().WithDefaultValue("Basic")
                .AddColumn("etc_formula").AsString().WithDefaultValue("Derived");
        }

        public override void Down()
        {
            Delete.Column("eac_formula").FromTable("tracked_teams");
            Delete.Column("etc_formula").FromTable("tracked_teams");
        }
    }
}
