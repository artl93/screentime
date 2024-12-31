using System.Runtime.InteropServices;

namespace ScreenTime
{
    public interface IIdleTimeDetector
    {

        public TimeSpan GetIdleTime();
    }
}