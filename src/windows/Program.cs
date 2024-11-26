using Microsoft.Win32;

System.DateTime startTime = System.DateTime.Now;
System.TimeSpan downTime = System.TimeSpan.Zero;
System.DateTime downTimeStart = System.DateTime.Now;
System.DateTime downTimeEnd = System.DateTime.Now;
System.DateTime now = System.DateTime.Now;
bool isLocked = false;

void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
{
    if (e.Reason == SessionSwitchReason.SessionLock)
    {
        isLocked = true;
        downTimeStart = System.DateTime.Now;
        Console.WriteLine("The session has been locked.");
    }
    else if (e.Reason == SessionSwitchReason.SessionUnlock)
    {
        isLocked = false;
        downTimeEnd = System.DateTime.Now;
        var lockTime = downTimeEnd - downTimeStart;
        downTime = downTime.Add(lockTime);
        Console.WriteLine("The session has been unlocked.");
        Console.WriteLine("The session was locked for " + (lockTime.TotalSeconds) + " seconds.");
    }
}

SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
Console.WriteLine("Listening for session switch events. Press any key to exit...");

await Task.Run(async () =>
{
    while (true)
    {
        now = DateTime.Now;
        await Task.Delay(1000);
        if (isLocked)
        {
            Console.WriteLine("The session is locked.");
            continue;
        }
        Console.WriteLine("Time logged in interactively: " + (now - startTime - downTime).ToString());
    }
});

 

