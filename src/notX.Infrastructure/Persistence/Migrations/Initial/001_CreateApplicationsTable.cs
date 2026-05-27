using FluentMigrator;

namespace notX.Infrastructure.Persistence.Migrations.Initial;

[Migration(1)]
public class CreateApplicationsTable : Migration
{
    public override void Up()
    {
        Create.Table("applications")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("api_key").AsString(100).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("applications");
    }
}