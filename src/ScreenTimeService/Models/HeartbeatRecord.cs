namespace ScreenTimeService.Models
{
    public class HeartbeatRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public required TimeSpan Duration { get; set; }
        public required ScreenTime.Common.UserState UserState { get; set; }
    }
}
