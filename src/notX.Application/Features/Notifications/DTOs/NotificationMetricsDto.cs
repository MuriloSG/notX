namespace notX.Application.Features.Notifications.DTOs;

public sealed record DashboardSnapshotDto(
    StatusCountsDto Status,
    TypeCountsDto Types,
    double SuccessRate,
    IReadOnlyList<TimeBucketDto> Last24h,
    IReadOnlyList<NotificationDto> Recent);

public sealed record StatusCountsDto(
    int Pending,
    int Processing,
    int Sent,
    int Failed,
    int Cancelled);

public sealed record TypeCountsDto(int Email, int Sms);

public sealed record TimeBucketDto(DateTime Hour, int Sent, int Failed);
