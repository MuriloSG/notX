using Dapper;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using AppEntity = notX.Domain.Entities.Application;

namespace notX.Infrastructure.Persistence.Repositories;

internal sealed class ApplicationRepository(IDbConnectionFactory connectionFactory)
    : IApplicationRepository
{
    public async Task InsertAsync(AppEntity application)
    {
        const string sql = """
            INSERT INTO applications (id, name, api_key, created_at)
            VALUES (@Id, @Name, @ApiKey, @CreatedAt)
            """;

        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new
        {
            application.Id,
            application.Name,
            application.ApiKey,
            application.CreatedAt
        });
    }

    public async Task<AppEntity?> GetByApiKeyAsync(string apiKey)
    {
        const string sql = """
            SELECT id, name, api_key AS ApiKey, created_at AS CreatedAt
            FROM applications
            WHERE api_key = @ApiKey
            """;

        using var connection = connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<AppEntity>(sql, new { ApiKey = apiKey });
    }
}
