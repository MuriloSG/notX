using notX.Domain.Common;

namespace notX.Domain.Entities;

public class Application : BaseEntity
{
    public string Name { get; internal set; } = default!;
    public string ApiKey { get; internal set; } = default!;
    public DateTime CreatedAt { get; internal set; }

    private Application() { }

    public static Application Create(string name, string apiKey) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        ApiKey = apiKey,
        CreatedAt = DateTime.UtcNow
    };
}
