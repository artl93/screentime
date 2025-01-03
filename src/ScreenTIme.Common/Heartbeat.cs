namespace ScreenTime.Common
{
    public record Heartbeat (DateTimeOffset Timestamp, TimeSpan Duration, UserState UserState);

    public class UserHeartbeat
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public UserState UserState { get; set; }
    }

    public class User
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string NameIdentifierId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }

    public class ClientConfiguration
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Platform { get; set; }
        public string? Architecture { get; set; }
    }
}
