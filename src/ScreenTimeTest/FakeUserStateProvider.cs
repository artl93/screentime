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
                ActivityState = ActivityState.Inactive;
            }

            public FakeUserStateProvider(string lastKnownDate, string duration)
            {
                LastKnownDate = DateTimeOffset.Parse(lastKnownDate);
                Duration = TimeSpan.Parse(duration);
                State = UserState.Okay;
                TimeMessageLastShown = LastKnownDate.AddDays(-7);
                ActivityState = ActivityState.Inactive;
            }

            public DateTimeOffset LastKnownDate { get; set; }
            public TimeSpan Duration { get; set;  }
            public UserState State { get; set; }
            public DateTimeOffset TimeMessageLastShown { get; set; }
            public ActivityState ActivityState { get; set; }

            public override void LoadState(out DateTimeOffset lastKnownTime, out TimeSpan duration, out UserState state, out DateTimeOffset timeMessageLastShown, out ActivityState activityState)
            {
                lastKnownTime = LastKnownDate;
                duration = Duration;
                state = State;
                timeMessageLastShown = TimeMessageLastShown;
                activityState = ActivityState;
            }
            public override void SaveState(DateTimeOffset lastKnownTime, TimeSpan duration, UserState state, DateTimeOffset timeMessageLastShown, ActivityState activityState)
            {
                LastKnownDate = lastKnownTime;
                Duration = duration;
                State = state;
                TimeMessageLastShown = timeMessageLastShown;
                ActivityState = activityState;

            }
        }

    }
}
