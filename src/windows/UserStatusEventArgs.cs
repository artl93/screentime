namespace ScreenTime
{
    public class UserStatusEventArgs
    {
        public UserStatus Status { get; init; }

        public DateTimeOffset DateTime { get; init; }
        public TimeSpan InteractiveTime { get; init; }

        public UserStatusEventArgs(UserStatus status, DateTimeOffset dateTime, TimeSpan interactiveTime)
        {
            this.Status = status;
            this.DateTime = dateTime;
            this.InteractiveTime = interactiveTime;
        }
    }
}