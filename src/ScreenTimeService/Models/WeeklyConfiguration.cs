namespace ScreenTimeService.Models
{
    public class WeeklyConfiguration
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required DailyConfiguration Default { get; set; }
        public DailyConfiguration? Sunday { get; set; }
        public DailyConfiguration? Monday { get; set; }
        public DailyConfiguration? Tuesday { get; set; }
        public DailyConfiguration? Wednesday { get; set; }
        public DailyConfiguration? Thursday { get; set; }
        public DailyConfiguration? Friday { get; set; }
        public DailyConfiguration? Saturday { get; set; }
    }
}
