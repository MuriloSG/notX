using System.Data;

namespace notX.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}