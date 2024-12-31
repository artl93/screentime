using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using ScreenTimeClient;
using Microsoft.Extensions.Time.Testing;

namespace ScreenTimeTest
{
    public class RegistryConfigurationProviderTests
    {
        [Fact]
        public async Task GetUserConfigurationForDayAsync_ReturnsConfiguration()
        {
            var _mockReader = new Mock<IUserConfigurationReader>();
            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader.Object, _mockTimeProvider);

            // Arrange
            var expectedConfig = new UserConfiguration("test");
            _mockReader.Setup(r => r.GetConfiguration()).Returns(expectedConfig);

            // Act
            var result = await _provider.GetUserConfigurationForDayAsync();

            // Assert
            Assert.Equal(expectedConfig, result);
        }


        [Fact]
        public async Task OnConfigurationChanged_EventNotTriggered()
        {
            var configurationA = new UserConfiguration("testA");
            var configurationB = new UserConfiguration("testA");
            var expectedConfiguration = new UserConfiguration("testA");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("2024-12-25 00:00:00"));


            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;
            
            // Act
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
            var configurationA = new UserConfiguration("testA");
            var configurationB = new UserConfiguration("testB");
            var expectedConfiguration = new UserConfiguration("testB");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("2024-12-25 00:00:00"));

            // Arrange



            var eventTriggered = false;
            _provider.OnConfigurationChanged += (sender, args) => eventTriggered = true;


            // Act
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));
            await _provider.SaveUserConfigurationForDayAsync(configurationB);
            _mockTimeProvider.Advance(TimeSpan.FromSeconds(20));

            Assert.True(eventTriggered);
            Assert.Equal(expectedConfiguration, await _provider.GetUserConfigurationForDayAsync());
        }


        [Fact]
        public async Task OnConfigurationChanged_EventTriggeredUsingWatcher()
        {
            var configurationA = new UserConfiguration("testA");
            var configurationB = new UserConfiguration("testB");
            var expectedConfiguration = new UserConfiguration("testB");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("2024-12-25 00:00:00"));

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
