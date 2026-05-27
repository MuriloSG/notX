namespace notX.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);
}
