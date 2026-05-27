namespace notX.Application.Features.Applications.DTOs;

public sealed record ApplicationDto(
    Guid Id,
    string Name,
    string ApiKey,
    DateTime CreatedAt);
