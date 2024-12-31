using System.Runtime.InteropServices;

namespace ScreenTimeClient
{
    public interface IIdleTimeDetector
    {

        public TimeSpan GetIdleTime();
    }
}