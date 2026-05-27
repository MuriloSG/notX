using AppEntity = notX.Domain.Entities.Application;

namespace notX.Application.Interfaces.Repositories;

public interface IApplicationRepository
{
    Task InsertAsync(AppEntity application);
    Task<AppEntity?> GetByApiKeyAsync(string apiKey);
}
