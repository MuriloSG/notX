namespace notX.Infrastructure.Settings;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = default!;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FromName { get; init; } = default!;
    public string FromEmail { get; init; } = default!;
}
