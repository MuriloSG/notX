using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using notX.Application.Interfaces;

namespace notX.Infrastructure.Persistence.Connections;

public class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly IConfiguration _configuration = configuration;

    public IDbConnection CreateConnection()
    {
        // Aspire injects the connection string as "notxdb"; fall back to "DefaultConnection" for local dev
        var connectionString =
            _configuration.GetConnectionString("notxdb")
            ?? _configuration.GetConnectionString("DefaultConnection");

        return new NpgsqlConnection(connectionString);
    }
}