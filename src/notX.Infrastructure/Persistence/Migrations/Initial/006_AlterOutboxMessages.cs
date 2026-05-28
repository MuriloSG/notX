using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(6)]
public class AlterOutboxMessages : Migration
{
    public override void Up()
    {
        Alter.Table("outbox_messages")
            .AddColumn("scheduled_at").AsDateTime().Nullable()
            .AddColumn("lock_token").AsGuid().Nullable()
            .AddColumn("locked_at").AsDateTime().Nullable();

        Create.Index("ix_outbox_messages_pending")
            .OnTable("outbox_messages")
            .OnColumn("processed_at").Ascending()
            .OnColumn("scheduled_at").Ascending()
            .OnColumn("lock_token").Ascending();
    }

    public override void Down()
    {
        Delete.Index("ix_outbox_messages_pending").OnTable("outbox_messages");
        Delete.Column("scheduled_at").FromTable("outbox_messages");
        Delete.Column("lock_token").FromTable("outbox_messages");
        Delete.Column("locked_at").FromTable("outbox_messages");
    }
}
