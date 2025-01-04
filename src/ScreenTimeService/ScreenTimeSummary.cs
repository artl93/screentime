namespace ScreenTimeService
{
    public class ScreenTimeSummary
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public required TimeSpan TotalDuration { get; set; }
        public required TimeSpan Extensions { get; set; }
    }
}
