using System.Diagnostics.CodeAnalysis;

namespace ScreenTime
{
    public record class UserConfiguration(
            string Name,
            int DailyLimitMinutes = 60,
            int WarningTimeMinutes = 10,
            int WarningIntervalSeconds = 60,
            int GraceMinutes = 5,
            string ResetTime = "06:00",
            bool DisableLock = false,
            int DelayLockSeconds = 10,
            bool EnableOnline = false,
            List<(DateTimeOffset, int)>? Extensions = null) : IEquatable<UserConfiguration>, IComparable<UserConfiguration>
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

        public int CompareTo(UserConfiguration? other)
        {
            throw new Exception();
        }

        virtual public bool Equals(UserConfiguration? other)
        {
            // throw new Exception();
            if (other == null)
            {
                return false;
            }
            if (Name != other.Name)
            {
                return false;
            }
            if (DailyLimitMinutes != other.DailyLimitMinutes)
            {
                return false;
            }
            if (WarningTimeMinutes != other.WarningTimeMinutes)
            {
                return false;
            }
            if (WarningIntervalSeconds != other.WarningIntervalSeconds)
            {
                return false;
            }
            if (GraceMinutes != other.GraceMinutes)
            {
                return false;
            }
            if (ResetTime != other.ResetTime)
            {
                return false;
            }
            if (DisableLock != other.DisableLock)
            {
                return false;
            }
            if (DelayLockSeconds != other.DelayLockSeconds)
            {
                return false;
            }
            if (EnableOnline != other.EnableOnline)
            {
                return false;
            }
            if (Extensions == null && other.Extensions == null)
            {
                return true;
            }
            if (Extensions == null || other.Extensions == null)
            {
                return false;
            }
            if (Extensions.Count != other.Extensions.Count)
            {
                return false;
            }
            var extensions1 = Extensions.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ToList();
            var extensions2 = other.Extensions.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ToList();
            for (var i = 0; i < extensions1.Count; i++)
            {
                if (extensions1[i].Item1 != extensions2[i].Item1)
                {
                    return false;
                }
                if (extensions1[i].Item2 != extensions2[i].Item2)
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            // test
            throw new Exception();
            return base.GetHashCode();
        }
    }
}