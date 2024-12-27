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
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 00:49 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", UserState.Okay, "00:49:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 00:50 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", UserState.Warn, "00:50:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:00 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:04 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", UserState.Error, "01:04:00")]
        [InlineData("2024/12/14 00:00 -8:00", "2024/12/14 01:05 -8:00", "2024/12/14 00:00 -8:00", "00:00:00", "00:00", UserState.Lock, "01:05:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/14 23:59 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", UserState.Okay, "00:29:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:00 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", UserState.Okay, "00:00:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:01 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", UserState.Okay, "00:01:00")]
        [InlineData("2024/12/14 23:30 -8:00", "2024/12/15 00:30 -8:00", "2024/12/14 23:30 -8:00", "00:00:00", "00:00", UserState.Okay, "00:30:00")]
        [InlineData("2024/12/15 00:00 -8:00", "2024/12/15 00:30 -8:00", "2024/12/14 23:30 -8:00", "00:30:00", "00:00", UserState.Okay, "00:30:00")]
        [InlineData("2024/12/15 00:00 -8:00", "2024/12/15 00:30 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", UserState.Okay, "00:30:00")] // note, this is a bogus case, but it should still work
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:20 -8:00", "2024/12/15 01:15 -8:00", "00:30:00", "00:00", UserState.Warn, "00:50:00")]
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:34 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", UserState.Error, "01:04:00")]
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:35 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", UserState.Lock, "01:05:00")]
        [InlineData("2024/12/15 02:00 -8:00", "2024/12/15 02:30 -8:00", "2024/12/15 01:00 -8:00", "00:30:00", "00:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/15 23:59 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", UserState.Warn, "00:59:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:00 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", UserState.Okay, "00:00:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:01 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", UserState.Okay, "00:01:00")]
        [InlineData("2024/12/15 23:30 -8:00", "2024/12/16 00:30 -8:00", "2024/12/15 23:30 -8:00", "00:30:00", "00:00", UserState.Okay, "00:30:00")]
        [InlineData("2024/12/16 01:30 -8:00", "2024/12/16 01:30 -8:00", "2024/12/16 01:00 -8:00", "01:00:00", "00:00", UserState.Error, "01:00:00")]

        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 00:49 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Okay, "00:49:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 00:50 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Warn, "00:50:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:00 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:04 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Error, "01:04:00")]
        [InlineData("2024/12/24 00:00 -8:00", "2024/12/24 01:05 -8:00", "2024/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Lock, "01:05:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/24 23:59 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", UserState.Okay, "00:29:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:00 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", UserState.Okay, "00:30:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:01 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", UserState.Okay, "00:31:00")]
        [InlineData("2024/12/24 23:30 -8:00", "2024/12/25 00:30 -8:00", "2024/12/24 23:30 -8:00", "00:00:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/25 00:00 -8:00", "2024/12/25 00:30 -8:00", "2024/12/24 23:30 -8:00", "00:30:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/25 00:00 -8:00", "2024/12/25 00:30 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", UserState.Okay, "00:30:00")] // note, this is a bogus case, but it should still work
        [InlineData("2024/12/25 01:00 -8:00", "2024/12/25 01:20 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", UserState.Warn, "00:50:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:34 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", UserState.Error, "01:04:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:35 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", UserState.Lock, "01:05:00")]
        [InlineData("2024/12/25 02:00 -8:00", "2024/12/25 02:30 -8:00", "2024/12/25 00:00 -8:00", "00:30:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/25 23:59 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", UserState.Warn, "00:59:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:00 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:01 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", UserState.Error, "01:01:00")]
        [InlineData("2024/12/25 23:30 -8:00", "2024/12/26 00:30 -8:00", "2024/12/25 23:30 -8:00", "00:30:00", "06:00", UserState.Lock, "01:30:00")]
        [InlineData("2024/12/26 01:30 -8:00", "2024/12/26 01:30 -8:00", "2024/12/26 01:00 -8:00", "01:00:00", "06:00", UserState.Error, "01:00:00")]


        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 05:49 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Okay, "00:49:00")]
        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 05:50 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Warn, "00:50:00")]
        [InlineData("2025/12/24 05:00 -8:00", "2025/12/24 06:00 -8:00", "2025/12/24 00:00 -8:00", "00:00:00", "06:00", UserState.Okay, "00:00:00")]

        [InlineData("2026/12/24 06:00 -8:00", "2026/12/24 07:00 -8:00", "2026/12/23 08:00 -8:00", "02:00:00", "06:00", UserState.Error, "01:00:00")]
        [InlineData("2026/12/25 06:00 -8:00", "2026/12/26 07:00 -8:00", "2026/12/23 08:00 -8:00", "1.00:00:00", "06:00", UserState.Error, "01:00:00")]

        public async Task TestGetInteractiveTime(
            string startString, string nowString, string lastKnownDate, string lastDuration, string resetTime,
            ScreenTime.UserState expectedStatus, string expectedDuration)
        {
            var start = DateTimeOffset.Parse(startString);
            var now = DateTimeOffset.Parse(nowString);
            var elapsed = now - start;
            ;

            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);

            var userStateProvider = new FakeUserStateProvider(lastKnownDate, lastDuration);

            var mockUserConfiguration = new UserConfiguration("test", ResetTime: resetTime);
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);


            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(elapsed);

            var result = await service.GetInteractiveTimeAsync();
            service.EndSession("test");
            await service.StopAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(TimeSpan.Parse(expectedDuration), result.LoggedInTime);
            Assert.Equal(expectedStatus, result.State);
        }

        [Theory]
        [InlineData(
            new string[]
            {
                "2027/01/01 06:00 -8:00,2027/01/01 06:30 -8:00, Okay",
                "2027/01/01 08:00 -8:00,2027/01/01 08:30 -8:00, Error",
            },
            UserState.Error, "01:00:00")]
        [InlineData(
            new string[]
            {
                "2027/01/02 06:00 -8:00,2027/01/02 06:30 -8:00, Okay",
                "2027/01/02 08:00 -8:00,2027/01/02 08:30 -8:00, Error",
                "2027/01/02 09:30 -8:00,2027/01/02 09:35 -8:00, Lock",
            },
            UserState.Lock, "01:05:00")]
        [InlineData(
            new string[]
            {
                "2027/01/03 06:00 -8:00,2027/01/03 06:30 -8:00, Okay",
                "2027/01/03 08:00 -8:00,2027/01/03 08:30 -8:00, Error",
                "2027/01/03 09:30 -8:00,2027/01/03 09:35 -8:00, Lock",
                "2027/01/04 09:30 -8:00,2027/01/04 09:35 -8:00, Okay",
            },
            UserState.Okay, "00:05:00")]
        [InlineData(
            new string[]
            {
                "2027/01/03 06:00 -8:00,2027/01/03 06:30 -8:00, Okay",
                "2027/01/03 08:00 -8:00,2027/01/03 08:30 -8:00, Error",
                "2027/01/03 09:30 -8:00,2027/01/03 09:35 -8:00, Lock",
                "2027/01/04 09:30 -8:00,2027/01/04 09:35 -8:00, Okay",
                "2027/01/04 10:30 -8:00,2027/01/04 11:30 -8:00, Lock",
            },
            UserState.Lock, "01:05:00")]
        public async Task TestGetInteractiveTimeSequences(
            string[] periodsArray,
            ScreenTime.UserState expectedStatus, string expectedDuration)
        {
            var periods = periodsArray
                .Select(p => p.Split(','))
                .Select(p => (DateTimeOffset.Parse(p[0]), DateTimeOffset.Parse(p[1]), Enum.Parse<UserState>(p[2])))
                .ToArray();

            FakeTimeProvider timeProvider = new();
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            timeProvider.SetUtcNow(DateTimeOffset.Parse("2027/01/01 00:00 -8:00"));

            var userStateProvider = new FakeUserStateProvider(timeProvider);

            var mockUserConfiguration = new UserConfiguration("test");
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);

            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            await service.StartAsync(CancellationToken.None);

            var expectedIntermediateDuration = TimeSpan.FromMinutes(0);

            foreach (var period in periods)
            {
                var start = period.Item1;
                var end = period.Item2;
                var expectedIntermediateStatus = period.Item3;
                var duration = end - start;

                timeProvider.SetUtcNow(start.UtcDateTime);
                service.StartSession("test");

                timeProvider.SetUtcNow(end.UtcDateTime);
                service.EndSession("test");
                await service.StopAsync(CancellationToken.None);

                expectedIntermediateDuration += duration;

                var intermediateResult = await service.GetInteractiveTimeAsync();

                Assert.NotNull(intermediateResult);

                // Assert.Equal(expectedIntermediateDuration, intermediateResult.LoggedInTime);
                Assert.Equal(expectedIntermediateStatus, intermediateResult.State);
            }

            var result = await service.GetInteractiveTimeAsync();

            Assert.NotNull(result);
            Assert.Equal(TimeSpan.Parse(expectedDuration), result.LoggedInTime);
            Assert.Equal(expectedStatus, result.State);
        }


        [Fact]
        public async Task TestOnDayRollover()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);

            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var mockUserConfiguration = new UserConfiguration("test");
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);


            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            var eventTriggered = false;
            service.OnDayRollover += (sender, args) => eventTriggered = true;

            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromDays(1));
            await service.StopAsync(CancellationToken.None);

            Assert.True(eventTriggered);
        }

        [Fact]
        public async Task TestOnTimeUpdate()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var mockUserConfiguration = new UserConfiguration("test");
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);

            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            var eventTriggered = false;
            service.OnTimeUpdate += (sender, args) => eventTriggered = true;
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await service.StopAsync(CancellationToken.None);
            Assert.True(eventTriggered);
        }

        [Fact]
        public async Task TestOnUserStatusChanged()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var mockUserConfiguration = new UserConfiguration("test");
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);
            
            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            var eventTriggered = false;
            service.OnUserStatusChanged += (sender, args) => eventTriggered = true;
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromHours(2));
            await service.StopAsync(CancellationToken.None);
            Assert.True(eventTriggered);
        }

        [Fact]
        public async Task TestOnMessageUpdateChanged()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var mockUserConfiguration = new UserConfiguration("test");
            Moq.Mock<IUserConfigurationProvider> mockUserConfigurationProvider = new();
            mockUserConfigurationProvider.Setup(m => m.GetUserConfigurationForDayAsync()).ReturnsAsync(mockUserConfiguration);

            using var service = new ScreenTimeLocalService(timeProvider, mockUserConfigurationProvider.Object, userStateProvider, null);
            var eventTriggered = false;
            service.OnMessageUpdate += (sender, args) => eventTriggered = true;
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await service.StopAsync(CancellationToken.None);
            Assert.True(eventTriggered);
        }

        [Fact]
        public async Task TestOnConfigurationChangedViaSave()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var configurationA = new UserConfiguration("testA");
            var configurationB = new UserConfiguration("testB", DailyLimitMinutes: 145);

            UserConfigurationProvider provider = new(new MockUserConfigurationReader(configurationA), timeProvider);

            using var service = new ScreenTimeLocalService(timeProvider, provider, userStateProvider, null);
            var eventTriggered = false;
            service.OnUserStatusChanged += (sender, args) => { 
                 eventTriggered = true;
                // test sequence 

            };
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromMinutes(120));
            Assert.Equal(UserState.Lock, service.GetUserState());
            await provider.SaveUserConfigurationForDayAsync(configurationB);
            Assert.True(eventTriggered);
            Assert.Equal(UserState.Okay, service.GetUserState());

            await service.StopAsync(CancellationToken.None);
            Assert.True(eventTriggered);
        }


        [Fact]
        public async Task TestOnExtensionChanged()
        {
            var start = DateTimeOffset.Parse("2024/12/14 00:00 -8:00");
            FakeTimeProvider timeProvider = new(start);
            timeProvider.SetLocalTimeZone(TimeProvider.System.LocalTimeZone);
            var userStateProvider = new FakeUserStateProvider(start.ToString(), "00:00:00");
            var configurationA = new UserConfiguration("testA");

            UserConfigurationProvider provider = new(new MockUserConfigurationReader(configurationA), timeProvider);

            using var service = new ScreenTimeLocalService(timeProvider, provider, userStateProvider, null);
            var eventTriggered = false;
            service.OnUserStatusChanged += (sender, args) => {
                eventTriggered = true;
                // test sequence 

            };
            await service.StartAsync(CancellationToken.None);
            service.StartSession("test");
            timeProvider.Advance(TimeSpan.FromMinutes(60));
            Assert.Equal(UserState.Error, service.GetUserState());
            await service.RequestExtensionAsync(5);
            Assert.True(eventTriggered);
            Assert.Equal(UserState.Warn, service.GetUserState());
            await service.RequestExtensionAsync(30);
            Assert.True(eventTriggered);
            Assert.Equal(UserState.Okay, service.GetUserState());
            var userStatus = await service.GetInteractiveTimeAsync();
            Assert.NotNull(userStatus);
            Assert.Equal(TimeSpan.FromMinutes(35), userStatus.ExtensionTime);
            Assert.Equal(TimeSpan.FromMinutes(60), userStatus.LoggedInTime);
            timeProvider.Advance(TimeSpan.FromMinutes(25));
            userStatus = await service.GetInteractiveTimeAsync();
            Assert.NotNull(userStatus);
            Assert.Equal(TimeSpan.FromMinutes(85), userStatus.LoggedInTime);
            Assert.Equal(UserState.Warn, service.GetUserState());



            await service.StopAsync(CancellationToken.None);
            Assert.True(eventTriggered);
        }
    }
}

