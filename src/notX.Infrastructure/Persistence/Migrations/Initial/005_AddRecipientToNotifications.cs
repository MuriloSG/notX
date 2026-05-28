using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(5)]
public class AddRecipientToNotifications : Migration
{
    public override void Up()
    {
        Alter.Table("notifications")
            .AddColumn("recipient").AsString(500).Nullable();
    }

    public override void Down()
    {
        Delete.Column("recipient").FromTable("notifications");
    }
}
