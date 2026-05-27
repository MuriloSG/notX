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
            .WithColumn("recipient").AsString(255).NotNullable()
            .WithColumn("subject").AsString(255).NotNullable()
            .WithColumn("body").AsString(int.MaxValue).NotNullable()

            .WithColumn("status").AsInt32().NotNullable()

            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("sent_at").AsDateTime().Nullable();
    }

    public override void Down()
    {
        Delete.Table("notifications");
    }
}