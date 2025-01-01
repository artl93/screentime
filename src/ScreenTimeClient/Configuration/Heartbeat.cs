namespace ScreenTimeClient.Configuration
{
    public class Heartbeat
    {
        public DateTimeOffset Timestamp { get; init; }
        public TimeSpan Duration { get; init; }
    }
}
