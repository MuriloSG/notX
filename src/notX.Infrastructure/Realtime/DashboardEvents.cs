using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace notX.Infrastructure.Realtime;

public static class DashboardEvents
{
    public const string ChannelPrefix = "notx:events:app:";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static RedisChannel Channel(Guid applicationId)
        => RedisChannel.Literal(ChannelPrefix + applicationId);

    public static Task PublishAsync(
        IConnectionMultiplexer redis,
        Guid applicationId,
        string type,
        object data)
    {
        var payload = JsonSerializer.Serialize(new { type, data }, JsonOptions);
        return redis.GetSubscriber().PublishAsync(Channel(applicationId), payload);
    }
}
