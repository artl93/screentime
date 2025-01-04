using Moq;
using ScreenTime.Common;
using Microsoft.Extensions.Time.Testing;
using ScreenTimeClient.Configuration;

namespace ScreenTimeTest
{
    public class RegistryConfigurationProviderTests
    {
        [Fact]
        public async Task GetUserConfigurationForDayAsync_ReturnsConfiguration()
        {
            var _mockReader = new Mock<IDailyConfigurationReader>();
            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new DailyConfigurationLocalProvider(_mockReader.Object, _mockTimeProvider);

            // Arrange
            var expectedConfig = new DailyConfiguration();
            _mockReader.Setup(r => r.GetConfiguration()).Returns(expectedConfig);

            var result = await _provider.GetUserConfigurationForDayAsync();

            Assert.Equal(expectedConfig, result);
        }


        [Fact]
        public async Task OnConfigurationChanged_EventNotTriggered()
        {
            var configurationA = new DailyConfiguration();
            var configurationB = new DailyConfiguration();
            var expectedConfiguration = new DailyConfiguration();
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new DailyConfigurationLocalProvider(_mockReader, _mockTimeProvider);

            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;
            
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));
            await _provider.SaveUserDailyConfigurationAsync(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
            Assert.False(eventTriggered);
        }

        // test that the update event fires when the configuration changes
        [Fact]
        public async Task OnConfigurationChanged_EventTriggeredUsingSave()
        {
            var configurationA = new DailyConfiguration();
            var configurationB = new DailyConfiguration(DailyLimitMinutes: 120);
            var expectedConfiguration = new DailyConfiguration(DailyLimitMinutes: 120);
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new DailyConfigurationLocalProvider(_mockReader, _mockTimeProvider);

            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;

            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));
            await _provider.SaveUserDailyConfigurationAsync(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.True(eventTriggered);
            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
        }


        [Fact]
        public async Task OnConfigurationChanged_EventTriggeredUsingWatcher()
        {
            var configurationA = new DailyConfiguration();
            var configurationB = new DailyConfiguration(DailyLimitMinutes:120);
            var expectedConfiguration = new DailyConfiguration(DailyLimitMinutes: 120);
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new DailyConfigurationLocalProvider(_mockReader, _mockTimeProvider);

            // Arrange



            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;

            // Act
            _mockReader.SetConfiguration(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.True(eventTriggered);
            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
        }

    }
}
