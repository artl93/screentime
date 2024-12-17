using Xunit;
using ScreenTime;
using System;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace ScreenTimeTest
{
    public partial class ScreenTimeLocalServiceTest
    {
        [Theory]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 00:49 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", Status.Okay, "00:49:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 00:50 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", Status.Warn, "00:50:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:00 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:04 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", Status.Error, "01:04:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:05 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", Status.Lock, "01:05:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/14 23:59 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", Status.Okay, "00:29:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:00 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", Status.Okay, "00:00:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:01 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", Status.Okay, "00:01:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:30 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", Status.Okay, "00:30:00")]
        [InlineData("2024/12/15 00:00 -8:00", "2024/12/15 00:30 -8:00", "2024/12/14 23:30 -8:00", "00:30:00", "00:00", Status.Okay, "00:30:00")]
        [InlineData("2024/12/15 00:00 -8:00", "2024/12/15 00:30 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", Status.Okay, "00:30:00")] // note, this is a bogus case, but it should still work
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:20 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", Status.Warn, "00:50:00")] 
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:34 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", Status.Error, "01:04:00")]
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:35 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", Status.Lock, "01:05:00")]
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:30 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/15 23:59 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", Status.Warn, "00:59:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:00 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", Status.Okay, "00:00:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:01 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", Status.Okay, "00:01:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:30 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", Status.Okay, "00:30:00")]
        [InlineData("2024/12/16 01:30 -8:00", "2024/12/16 01:30 -8:00", "2024/12/16 01:00 -8:00", "01:00:00", "00:00", Status.Error, "01:00:00")]

        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 00:49 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Okay, "00:49:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 00:50 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Warn, "00:50:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:00 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:04 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Error, "01:04:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:05 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Lock, "01:05:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/24 23:59 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", Status.Okay, "00:29:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:00 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", Status.Okay, "00:30:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:01 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", Status.Okay, "00:31:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:30 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/25 00:00 -8:00", "2024/12/25 00:30 -8:00", "2024/12/24 23:30 -8:00", "00:30:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/25 00:00 -8:00", "2024/12/25 00:30 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", Status.Okay, "00:30:00")] // note, this is a bogus case, but it should still work
        [InlineData("2024/12/25 01:00 -8:00", "2024/12/25 01:20 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", Status.Warn, "00:50:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:34 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", Status.Error, "01:04:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:35 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", Status.Lock, "01:05:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:30 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/25 23:59 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", Status.Warn, "00:59:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:00 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:01 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", Status.Error, "01:01:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:30 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", Status.Lock, "01:30:00")]
        [InlineData("2024/12/26 01:30 -8:00", "2024/12/26 01:30 -8:00", "2024/12/26 01:00 -8:00", "01:00:00", "06:00", Status.Error, "01:00:00")]


        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 05:49 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Okay, "00:49:00")]
        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 05:50 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Warn, "00:50:00")]
        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 06:00 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", Status.Okay, "00:00:00")]

        [InlineData("2026/12/24 06:00 -8:00", "2026/12/24 07:00 -8:00", "2026/12/23 08:00 -8:00", "02:00:00", "06:00", Status.Error, "01:00:00")]
        [InlineData("2026/12/25 06:00 -8:00", "2026/12/26 07:00 -8:00", "2026/12/23 08:00 -8:00", "1.00:00:00", "06:00", Status.Error, "01:00:00")]

        async Task TestGetInteractiveTime(
            string startString, string nowString, string lastKnownDate, string lastDuration, string resetTime,
            ScreenTime.Status expectedStatus, string expectedDuration)
//            int dailyLimitMinutes, int warningTimeMinutes, int warningIntervalSeconds, int graceMinutes)
        {
            var start = DateTimeOffset.Parse(startString);
            var now = DateTimeOffset.Parse(nowString);
            var elapsed = now - start;
;

            FakeTimeProvider timeProvider = new FakeTimeProvider(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            
            var userStateProvider = new FakeUserStateProvider(lastKnownDate, lastDuration);

            var mockUserConfiguration = new UserConfiguration(Guid.NewGuid(), "test", ResetTime: resetTime);

            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfiguration, userStateProvider);
            service.StartSessionAsync();
            timeProvider.Advance(elapsed);

            var result = await service.GetInteractiveTimeAsync();
            service.EndSessionAsync();

            Assert.NotNull(result);
            Assert.Equal(TimeSpan.Parse(expectedDuration), result.LoggedInTime);
            Assert.Equal(expectedStatus, result.Status);
        }

    }
}
