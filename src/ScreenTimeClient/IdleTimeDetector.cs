using System.Runtime.InteropServices;

namespace ScreenTimeClient
{
    public class IdleTimeDetector : IIdleTimeDetector
    {

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO lastInputInfo);

        public TimeSpan GetIdleTime()
        {
            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(idleTime);
        }
    }
}

