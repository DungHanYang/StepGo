using StepGo.Shared.Application;

namespace StepGo.Shared.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
