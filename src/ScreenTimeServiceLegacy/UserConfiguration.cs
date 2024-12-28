public class UserConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int DailyLimitMinutes { get; set; } = 60;
    public int WarningTimeMinutes { get; set; } = 10;
    public int WarningIntervalSeconds { get; set; } = 60;
    public int GraceMinutes { get; set; } = 5;
}
