using System.Runtime.InteropServices;

namespace ScreenTime
{
    public class IdleTimeDetector
    {

        public static TimeSpan GetIdleTime()
        {
            var lastInputInfo = new Win32Interop.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            Win32Interop.GetLastInputInfo(ref lastInputInfo);

            uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(idleTime);
        }
    }
}

