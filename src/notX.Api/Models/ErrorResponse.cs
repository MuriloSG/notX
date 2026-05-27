namespace notX.Api.Models;

/// <summary>Represents a structured error returned by the API.</summary>
public sealed record ErrorResponse(string Code, string Message);
