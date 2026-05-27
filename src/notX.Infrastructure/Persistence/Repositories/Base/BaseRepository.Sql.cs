namespace notX.Infrastructure.Persistence.Repositories.Base;

public abstract partial class BaseRepository<T>
{
    protected string SqlGetById =>
        $"SELECT * FROM {_tableName} WHERE Id = @Id";

    protected string SqlGetPaged =>
        $@"
        SELECT * FROM {_tableName}
        ORDER BY Id
        OFFSET @Offset LIMIT @PageSize";

    protected string SqlCount =>
        $"SELECT COUNT(*) FROM {_tableName}";

    protected string SqlInsert =>
        $"INSERT INTO {_tableName} VALUES (@Entity)";

    protected string SqlUpdate =>
        $"UPDATE {_tableName} SET @Entity WHERE Id = @Id";

    protected string SqlDelete =>
        $"DELETE FROM {_tableName} WHERE Id = @Id";

    protected string SqlDeleteMany =>
        $"DELETE FROM {_tableName} WHERE Id = ANY(@Ids)";
}