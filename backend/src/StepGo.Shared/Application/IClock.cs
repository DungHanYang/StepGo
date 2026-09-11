namespace StepGo.Shared.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
