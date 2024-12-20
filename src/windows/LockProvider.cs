﻿using System.Management;
using System.Runtime.InteropServices;

namespace ScreenTime
{
    public class LockProvider
    {
        [DllImport("user32.dll")]
        static extern bool LockWorkStation();

        public async void Lock()
        {
            await Task.Delay(10000);
            LockWorkStation();
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


/*

Application.ApplicationExit += (s, e) =>
{
    client.EndSessionAsync();
    icon.Visible = false;
    icon.Dispose();
    task.Dispose();
};

// make sure the main window loop runs
Application.Run(new HiddenForm(task));
*/