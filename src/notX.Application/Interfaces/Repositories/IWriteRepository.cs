namespace notX.Application.Interfaces.Repositories;

public interface IWriteRepository<T> where T : class
{
    Task CreateAsync(T entity);
    Task CreateManyAsync(IEnumerable<T> entities);

    Task UpdateAsync(T entity);
    Task UpdateManyAsync(IEnumerable<T> entities);

    Task DeleteAsync(Guid id);
    Task DeleteManyAsync(IEnumerable<Guid> ids);
}