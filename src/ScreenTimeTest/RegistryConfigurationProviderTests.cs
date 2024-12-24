using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using ScreenTime;
using Microsoft.Extensions.Time.Testing;

namespace ScreenTimeTest
{
    public class RegistryConfigurationProviderTests
    {
        private bool _disposedValue;

        [Fact]
        public async Task GetUserConfigurationForDayAsync_ReturnsConfiguration()
        {
            var _mockReader = new Mock<IUserConfigurationReader>();
            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader.Object, _mockTimeProvider);

            // Arrange
            var expectedConfig = new UserConfiguration(Guid.NewGuid(), "test");
            _mockReader.Setup(r => r.GetConfiguration()).Returns(expectedConfig);

            // Act
            var result = await _provider.GetUserConfigurationForDayAsync();

            // Assert
            Assert.Equal(expectedConfig, result);
        }



        [Fact]
        public async Task SaveUserConfigurationForDayAsync_SavesConfiguration()
        {
            var _mockReader = new Mock<IUserConfigurationReader>();
            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader.Object, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("00:00:00"));

            // Arrange
            var expectedConfig = new UserConfiguration(Guid.NewGuid(), "test");
            _mockReader.Setup(r => r.SetConfiguration(expectedConfig));

            // Act
            await _provider.SaveUserConfigurationForDayAsync(expectedConfig);

            // Assert
            _mockReader.Verify(r => r.SetConfiguration(expectedConfig), Times.Once);
        }

        [Fact]
        public async Task OnConfigurationChanged_EventNotTriggered()
        {
            var guidA = Guid.Parse("07a78d36-c409-4805-8b56-e7cb2368bccf");
            var guidB = Guid.Parse("df991e7f-12c4-4c1e-8bb6-591065103f61");
            var configurationA = new UserConfiguration(guidA, "test");
            var configurationB = new UserConfiguration(guidA, "test");
            var expectedConfiguration = new UserConfiguration(guidA, "test");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("00:00:00"));


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
            var guidA = Guid.Parse("07a78d36-c409-4805-8b56-e7cb2368bccf");
            var guidB = Guid.Parse("df991e7f-12c4-4c1e-8bb6-591065103f61");
            var configurationA = new UserConfiguration(guidA, "test");
            var configurationB = new UserConfiguration(guidB, "test");
            var expectedConfiguration = new UserConfiguration(guidB, "test");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("00:00:00"));

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
            var guidA = Guid.Parse("07a78d36-c409-4805-8b56-e7cb2368bccf");
            var guidB = Guid.Parse("df991e7f-12c4-4c1e-8bb6-591065103f61");
            var configurationA = new UserConfiguration(guidA, "test");
            var configurationB = new UserConfiguration(guidB, "test");
            var expectedConfiguration = new UserConfiguration(guidB, "test");
            var _mockReader = new MockUserConfigurationReader(configurationA);

            var _mockTimeProvider = new FakeTimeProvider();
            using var _provider = new UserConfigurationProvider(_mockReader, _mockTimeProvider);
            _mockTimeProvider.SetUtcNow(DateTime.Parse("00:00:00"));

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
