namespace ScreenTime.Common
{
    public class Heartbeat
    {
        public DateTimeOffset Timestamp { get; init; }
        public TimeSpan Duration { get; init; }
    }
}
