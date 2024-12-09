using System.Runtime.InteropServices;

public class IdleTimeDetector
{

    public static TimeSpan GetIdleTime()
    {
        var lastInputInfo = new Windows.LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        Windows.GetLastInputInfo(ref lastInputInfo);

        uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
        return TimeSpan.FromMilliseconds(idleTime);
    }
}

public class Windows
{
    [DllImport("user32.dll")]
    public static extern bool LockWorkStation();


    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
}

