namespace notX.Application.Interfaces.Repositories;

public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize);

    Task<int> CountAsync();
}