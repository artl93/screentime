using System.Management;
using System.Runtime.InteropServices;

namespace ScreenTime
{
    public class SystemLockStateService
    {
        [DllImport("user32.dll")]
        static extern bool LockWorkStation();

        public void Lock()
        {
            Task.Delay(1000).Wait();
            // LockWorkStation();
            // TODO: reenable locking
        }

        public void LogOut()
        {
            ManagementBaseObject? mboShutdown;
            ManagementClass mcWin32 = new ("Win32_OperatingSystem");
            mcWin32.Get();
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            mboShutdownParams["Flags"] = "2";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances().Cast<ManagementObject>())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
            }
        }
    }
}

