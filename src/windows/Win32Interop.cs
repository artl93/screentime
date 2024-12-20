using System.Runtime.InteropServices;

namespace ScreenTime
{
    public class Win32Interop
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
}

