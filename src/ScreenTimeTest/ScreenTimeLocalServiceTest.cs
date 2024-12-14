using Xunit;
using ScreenTime;
using System;
using System.Threading.Tasks;
using Moq;

namespace ScreenTimeTest
{
    public class ScreenTimeLocalServiceTest
    {
        private ScreenTimeLocalService _service;

        void TestGetInteractiveTime(DateTimeOffset start, DateTimeOffset now, TimeSpan elapsed, ScreenTime.Status status)
        {
            var timeProviderMock = new Mock<TimeProvider>(1);
            timeProviderMock.Setup(x => x.GetUtcNow()).Returns(now);

            var mockUserConfiguration = new UserConfiguration(Guid.NewGuid(), "Test", 60, 10, 60, 5);

            var service = new ScreenTimeLocalService(timeProviderMock.Object, mockUserConfiguration);

        }

    }
}
