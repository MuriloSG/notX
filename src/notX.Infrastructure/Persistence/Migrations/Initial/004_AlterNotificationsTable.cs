using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(4)]
public class AlterNotificationsTable : Migration
{
    public override void Up()
    {
        Delete.Column("recipient").FromTable("notifications");
        Delete.Column("subject").FromTable("notifications");
        Delete.Column("body").FromTable("notifications");

        Alter.Table("notifications")
            .AddColumn("title").AsString(500).NotNullable().WithDefaultValue("")
            .AddColumn("content").AsCustom("text").NotNullable().WithDefaultValue("")
            .AddColumn("scheduled_at").AsDateTime().Nullable();

        Create.Index("ix_notifications_application_id")
            .OnTable("notifications").OnColumn("application_id");

        Create.Index("ix_notifications_status")
            .OnTable("notifications").OnColumn("status");
    }

    public override void Down()
    {
        Delete.Index("ix_notifications_status").OnTable("notifications");
        Delete.Index("ix_notifications_application_id").OnTable("notifications");

        Delete.Column("title").FromTable("notifications");
        Delete.Column("content").FromTable("notifications");
        Delete.Column("scheduled_at").FromTable("notifications");

        Alter.Table("notifications")
            .AddColumn("recipient").AsString(255).NotNullable().WithDefaultValue("")
            .AddColumn("subject").AsString(255).NotNullable().WithDefaultValue("")
            .AddColumn("body").AsCustom("text").NotNullable().WithDefaultValue("");
    }
}
