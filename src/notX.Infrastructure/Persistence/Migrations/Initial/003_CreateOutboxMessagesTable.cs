using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(3)]
public class CreateOutboxMessagesTable : Migration
{
    public override void Up()
    {
        Create.Table("outbox_messages")
            .WithColumn("id").AsGuid().PrimaryKey()

            .WithColumn("type").AsString(200).NotNullable()
            .WithColumn("payload").AsCustom("text").NotNullable()

            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("processed_at").AsDateTime().Nullable()

            .WithColumn("retry_count").AsInt32().NotNullable()
            .WithColumn("error").AsString(int.MaxValue).Nullable();
    }

    public override void Down()
    {
        Delete.Table("outbox_messages");
    }
}