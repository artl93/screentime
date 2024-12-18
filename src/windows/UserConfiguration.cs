namespace ScreenTime
{
    public record class UserConfiguration(Guid Id, string Name, int DailyLimitMinutes = 60, int WarningTimeMinutes = 10, int WarningIntervalSeconds = 60, int GraceMinutes = 5, string ResetTime = "06:00");

}