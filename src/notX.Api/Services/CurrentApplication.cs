using notX.Application.Interfaces;

namespace notX.Api.Services;

public sealed class CurrentApplication : ICurrentApplication
{
    public Guid ApplicationId { get; private set; }
    public string ApiKey { get; private set; } = default!;

    public void Set(Guid applicationId, string apiKey)
    {
        ApplicationId = applicationId;
        ApiKey = apiKey;
    }
}
