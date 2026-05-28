using notX.Shared.Results;

namespace notX.Application.Interfaces;

public interface IEmailService
{
    Task<Result> SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
