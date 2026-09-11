using StepGo.Application.Common;

namespace StepGo.Infrastructure.Aws;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
