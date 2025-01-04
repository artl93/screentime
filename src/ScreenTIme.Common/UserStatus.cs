namespace ScreenTime.Common
{
    public record UserStatus(TimeSpan LoggedInTime, string Icon, string Action, UserState State, TimeSpan DailyTimeLimit, TimeSpan ExtensionTime);

}