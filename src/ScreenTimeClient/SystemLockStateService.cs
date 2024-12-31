using System.Management;
using System.Runtime.InteropServices;

namespace ScreenTimeClient
{
    public partial class SystemLockStateService : ILockStateService
    {
        [LibraryImport("user32.dll", EntryPoint = "LockWorkStation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static private partial bool LockWorkStation();

        public void Lock()
        {
            // leave this safety delay in place to prevent the lock from happening too quickly
            LockWorkStation();
        }

        public void Logout()
        {
            ManagementBaseObject? mboShutdown;
            ManagementClass mcWin32 = new("Win32_OperatingSystem");
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

