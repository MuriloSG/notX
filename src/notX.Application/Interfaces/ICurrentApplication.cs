namespace notX.Application.Interfaces;

public interface ICurrentApplication
{
    Guid ApplicationId { get; }
    string ApiKey { get; }
}
