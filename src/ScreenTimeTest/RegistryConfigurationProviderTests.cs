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
            var _mockReader = new Mock<IUserConfigurationReader>();
            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new LocalUserConfigurationProvider(_mockReader.Object, _mockTimeProvider);

            // Arrange
            var expectedConfig = new UserConfiguration();
            _mockReader.Setup(r => r.GetConfiguration()).Returns(expectedConfig);

            var result = await _provider.GetUserConfigurationForDayAsync();

            Assert.Equal(expectedConfig, result);
        }


        [Fact]
        public async Task OnConfigurationChanged_EventNotTriggered()
        {
            var configurationA = new UserConfiguration();
            var configurationB = new UserConfiguration();
            var expectedConfiguration = new UserConfiguration();
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new LocalUserConfigurationProvider(_mockReader, _mockTimeProvider);

            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;
            
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));
            await _provider.SaveUserConfigurationForDayAsync(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
            Assert.False(eventTriggered);
        }

        // test that the update event fires when the configuration changes
        [Fact]
        public async Task OnConfigurationChanged_EventTriggeredUsingSave()
        {
            var configurationA = new UserConfiguration();
            var configurationB = new UserConfiguration(DailyLimitMinutes: 120);
            var expectedConfiguration = new UserConfiguration(DailyLimitMinutes: 120);
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new LocalUserConfigurationProvider(_mockReader, _mockTimeProvider);

            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;

            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));
            await _provider.SaveUserConfigurationForDayAsync(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.True(eventTriggered);
            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
        }


        [Fact]
        public async Task OnConfigurationChanged_EventTriggeredUsingWatcher()
        {
            var configurationA = new UserConfiguration();
            var configurationB = new UserConfiguration(DailyLimitMinutes:120);
            var expectedConfiguration = new UserConfiguration(DailyLimitMinutes: 120);
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-12-25 00:00:00"));
            using var _provider = new LocalUserConfigurationProvider(_mockReader, _mockTimeProvider);

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
