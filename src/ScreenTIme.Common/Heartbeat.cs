namespace ScreenTime.Common
{
    public record Heartbeat(DateTimeOffset Timestamp, TimeSpan Duration, UserState UserState);

    public record User (Guid Id, string Name, string Email);
}
