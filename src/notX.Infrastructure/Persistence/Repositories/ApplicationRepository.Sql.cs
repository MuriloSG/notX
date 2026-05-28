namespace notX.Infrastructure.Persistence.Repositories;

internal sealed partial class ApplicationRepository
{
    private const string SqlInsert = """
        INSERT INTO applications (id, name, api_key, created_at)
        VALUES (@Id, @Name, @ApiKey, @CreatedAt)
        """;

    private const string SqlGetByApiKey = """
        SELECT id, name, api_key AS ApiKey, created_at AS CreatedAt
        FROM applications
        WHERE api_key = @ApiKey
        """;
}
