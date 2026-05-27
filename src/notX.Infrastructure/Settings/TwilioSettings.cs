namespace notX.Infrastructure.Settings;

public sealed class TwilioSettings
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; init; } = default!;
    public string AuthToken { get; init; } = default!;
    public string FromNumber { get; init; } = default!;
}
