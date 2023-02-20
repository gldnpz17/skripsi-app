using FluentMigrator;

namespace SkripsiAppBackend.Persistence.Migrations
{
    [Migration(2)]
    public class AddDeadline : Migration
    {
        public override void Up()
        {
            Alter.Table("tracked_teams")
                .AddColumn("deadline").AsDateTime().Nullable();
        }

        public override void Down()
        {
            Delete.Column("deadline").FromTable("tracked_teams");
        }
    }
}
