using Dapper;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using AppEntity = notX.Domain.Entities.Application;

namespace notX.Infrastructure.Persistence.Repositories;

internal sealed partial class ApplicationRepository(IDbConnectionFactory connectionFactory)
    : IApplicationRepository
{
    public async Task InsertAsync(AppEntity application)
    {
        using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(SqlInsert, new
        {
            application.Id,
            application.Name,
            application.ApiKey,
            application.CreatedAt
        });
    }

    public async Task<AppEntity?> GetByApiKeyAsync(string apiKey)
    {
        using var connection = connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<AppEntity>(SqlGetByApiKey, new { ApiKey = apiKey });
    }
}
