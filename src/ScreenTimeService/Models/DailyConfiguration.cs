namespace ScreenTimeService.Models
{
    public class DailyConfiguration
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int DailyLimitMinutes { get; set; }
        public int GraceMinutes { get; set; }
        public int WarningIntervalSeconds { get; set; }
        public int WarningTimeMinutes { get; set; }
    }
}
