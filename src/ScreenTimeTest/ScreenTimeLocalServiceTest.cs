using Xunit;
using ScreenTime;
using System;
using System.Threading.Tasks;

namespace ScreenTimeTest
{
    [TestFixture]
    public class ScreenTimeLocalServiceTest
    {
        private ScreenTimeLocalService _service;

        [SetUp]
        public void Setup()
        {
            _service = new ScreenTimeLocalService();
        }

        [Test]
        public void TestGetScreenTime()
        {
            var result = _service.GetScreenTime();
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TimeSpan>(result);
        }

        [Test]
        public void TestSetScreenTime()
        {
            var newTime = new TimeSpan(2, 0, 0);
            _service.SetScreenTime(newTime);
            var result = _service.GetScreenTime();
            Assert.AreEqual(newTime, result);
        }

        [Test]
        public void TestResetScreenTime()
        {
            _service.ResetScreenTime();
            var result = _service.GetScreenTime();
            Assert.AreEqual(TimeSpan.Zero, result);
        }

        [Test]
        public async Task TestEndSessionAsync()
        {
            await _service.StartSessionAsync();
            await _service.EndSessionAsync();
            var result = await _service.GetInteractiveTimeAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(UserStatus.Inactive, result);
        }

        [Test]
        public async Task TestStartSessionAsync()
        {
            await _service.StartSessionAsync();
            var result = await _service.GetInteractiveTimeAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(UserStatus.Active, result);
        }

        [Test]
        public void TestSaveState()
        {
            var lastKnownTime = DateTimeOffset.Now;
            var duration = new TimeSpan(1, 0, 0);
            _service.SaveState(lastKnownTime, duration);
            var result = _service.GetScreenTime();
            Assert.AreEqual(duration, result);
        }

        [Test]
        public void TestResetAtMidnight()
        {
            var initialTime = new TimeSpan(23, 59, 0);
            _service.SetScreenTime(initialTime);
            var result = _service.GetScreenTime();
            Assert.AreEqual(initialTime, result);

            // Simulate passing of time to midnight
            var newTime = new TimeSpan(0, 1, 0);
            _service.SetScreenTime(newTime);
            result = _service.GetScreenTime();
            Assert.AreEqual(TimeSpan.Zero, result);
        }
    }
}
