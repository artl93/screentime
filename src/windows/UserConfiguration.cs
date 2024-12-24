namespace ScreenTime
{
    public record class UserConfiguration(
        Guid Id,
        string Name,
        int DailyLimitMinutes = 60,
        int WarningTimeMinutes = 10,
        int WarningIntervalSeconds = 60,
        int GraceMinutes = 5,
        string ResetTime = "06:00",
        bool DisableLock = false,
        int DelayLockSeconds = 10,
        List<(DateTimeOffset, int)>? Extensions = null)
    {
        public TimeSpan TotalTimeAllowed 
        { 
            get 
            { 
                var timeLimit = TimeSpan.FromMinutes(DailyLimitMinutes);
                if (Extensions != null)
                foreach (var extension in Extensions)
                {
                        timeLimit += TimeSpan.FromMinutes(extension.Item2);
                }
                return timeLimit; 
            } 

        }

        public TimeSpan TotalExtensions
        {
            get
            {
                if (Extensions == null)
                {
                    return TimeSpan.Zero;
                }
                var total = TimeSpan.Zero;
                foreach (var extension in Extensions)
                {
                    total += TimeSpan.FromMinutes(extension.Item2);
                }
                return total;
            }
        }

    }
}