using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(2)]
public class CreateNotificationsTable : Migration
{
    public override void Up()
    {
        Create.Table("notifications")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("application_id").AsGuid().NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("content").AsCustom("text").NotNullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("scheduled_at").AsDateTime().Nullable()
            .WithColumn("sent_at").AsDateTime().Nullable();

        Create.Index("ix_notifications_application_id")
            .OnTable("notifications").OnColumn("application_id");

        Create.Index("ix_notifications_status")
            .OnTable("notifications").OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("notifications");
    }
}
