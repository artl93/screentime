namespace ScreenTime
{
    public record UserStatus(TimeSpan LoggedInTime, string Icon, string Action, Status Status, TimeSpan DailyTimeLimit);

}