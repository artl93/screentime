using ScreenTime;
using System;

namespace ScreenTimeTest
{
    public partial class ScreenTimeLocalServiceTest
    {
        class FakeUserStateProvider : UserStateProvider
        {
            public FakeUserStateProvider(TimeProvider timeProvider)
            {
                LastKnownDate = timeProvider.GetUtcNow();
                Duration = TimeSpan.Zero;
                State = UserState.Okay;
                TimeMessageLastShown = timeProvider.GetUtcNow().AddDays(-7);
            }

            public FakeUserStateProvider(string lastKnownDate, string duration)
            {
                LastKnownDate = DateTimeOffset.Parse(lastKnownDate);
                Duration = TimeSpan.Parse(duration);
                State = UserState.Okay;
                TimeMessageLastShown = LastKnownDate.AddDays(-7);
            }

            public DateTimeOffset LastKnownDate { get; set; }
            public TimeSpan Duration { get; set;  }
            public UserState State { get; set; }
            public DateTimeOffset TimeMessageLastShown { get; set; }

            public override void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration, out UserState state, out DateTimeOffset timeMessageLastShown)
            {
                lastKnownTime = LastKnownDate;
                duration = Duration;
                state = State;
                timeMessageLastShown = TimeMessageLastShown;
            }
            public override void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration, UserState state, DateTimeOffset timeMessageLastShown)
            {
                LastKnownDate = lastKnownTime;
                Duration = duration;
                State = state;
                TimeMessageLastShown = timeMessageLastShown;
            }
        }

    }
}
