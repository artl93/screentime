namespace ScreenTime.Common
{
    public record Heartbeat (DateTimeOffset Timestamp, TimeSpan Duration, UserState UserState);
}
