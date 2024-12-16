using ScreenTime;

namespace ScreenTimeTest
{
    public partial class ScreenTimeLocalServiceTest
    {
        class FakeUserStateProvider : UserStateProvider
        {
            public FakeUserStateProvider(string lastKnownDate, string duration)
            {
                LastKnownDate = DateTimeOffset.Parse(lastKnownDate);
                Duration = TimeSpan.Parse(duration);
            }

            public DateTimeOffset LastKnownDate { get; set; }
            public TimeSpan Duration { get; set;  }

            public override void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration)
            {
                lastKnownTime = LastKnownDate;
                duration = Duration;
            }
            public override void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration)
            {
                LastKnownDate = lastKnownTime;
                Duration = duration;
            }
        }

    }
}
