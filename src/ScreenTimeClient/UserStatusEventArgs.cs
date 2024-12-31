using ScreenTimeClient;

namespace ScreenTimeClient
{
    public class UserStatusEventArgs(UserStatus status, DateTimeOffset dateTime, TimeSpan interactiveTime)
    {
        public UserStatus Status { get; init; } = status;

        public DateTimeOffset DateTime { get; init; } = dateTime;
        public TimeSpan InteractiveTime { get; init; } = interactiveTime;
    }
}