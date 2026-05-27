using System.Data;
using Dapper;
using notX.Application.Interfaces;

namespace notX.Infrastructure.Persistence.Repositories.Base;

public abstract partial class BaseRepository<T>(IDbConnectionFactory connectionFactory, string tableName) where T : class
{
    protected readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    protected readonly string _tableName = tableName;

    protected IDbConnection Connection => _connectionFactory.CreateConnection();

    public virtual async Task<T?> GetByIdAsync(Guid id)
        => await Connection.QueryFirstOrDefaultAsync<T>(SqlGetById, new { Id = id });

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        return await Connection.QueryAsync<T>(SqlGetPaged, new { Offset = offset, PageSize = pageSize });
    }

    public virtual async Task<int> CountAsync()
        => await Connection.ExecuteScalarAsync<int>(SqlCount);

    public virtual async Task CreateAsync(T entity)
        => await Connection.ExecuteAsync(SqlInsert, entity);

    public virtual async Task CreateManyAsync(IEnumerable<T> entities)
        => await Connection.ExecuteAsync(SqlInsert, entities);

    public virtual async Task UpdateAsync(T entity)
        => await Connection.ExecuteAsync(SqlUpdate, entity);

    public virtual async Task DeleteAsync(Guid id)
        => await Connection.ExecuteAsync(SqlDelete, new { Id = id });

    public virtual async Task DeleteManyAsync(IEnumerable<Guid> ids)
        => await Connection.ExecuteAsync(SqlDeleteMany, new { Ids = ids.ToArray() });
}