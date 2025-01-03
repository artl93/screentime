public class Configuration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; } = string.Empty;
    public DateTimeOffset ModifiedDate { get; set; } = DateTimeOffset.UtcNow;
    public required int[] DaysOfWeek { get; set; } = Array.Empty<int>();
    public required ScreenTime.Common.DailyConfiguration UserConfiguration { get; set; } = new ScreenTime.Common.DailyConfiguration();
}