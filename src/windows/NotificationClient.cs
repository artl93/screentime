using ScreenTime;

public class NotificationClient
{
    private NotifyIcon icon;

    public NotificationClient(IScreenTimeStateClient client)
    {
        icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = new ContextMenuStrip()
        };
        icon.ContextMenuStrip.Items.Add("Reset", null, (s, e) => { client.Reset(); });
        icon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { icon.Visible = false; Environment.Exit(0); });
        icon.Visible = true;
        icon.Text = "Connecting...";
        client.OnMessageUpdate += (s, e) => ShowMessage(e.Message);
        client.OnUserStatusChanged += (s, e) => UpdateTooltip(e.Status, e.InteractiveTime);
        client.OnDayRollover += (s, e) => ShowMessage(e.Message);
        client.OnTimeUpdate += (s, e) => UpdateTooltip(e.Status, e.InteractiveTime);

    }

    private void UpdateTooltip(UserStatus status, TimeSpan interactiveTime)
    {
        var humanizedUptime = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.LoggedInTime, 2);
        var humanizedDailyLimit = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.DailyTimeLimit, 2);

        icon.Text = $"{status.Icon} Interactive time: {humanizedUptime} out of {humanizedDailyLimit}.";
        
        icon.Icon = status.State switch
        {
            UserState.Okay => SystemIcons.Information,
            UserState.Warn => SystemIcons.Warning,
            UserState.Error => SystemIcons.Error,
            UserState.Lock => SystemIcons.Shield,
            _ => SystemIcons.Application
        };
    }

    public void ShowMessage(UserMessage message)
    {
        icon.BalloonTipTitle = message.Title ?? string.Empty;
        icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
        Task.Run(() => icon.ShowBalloonTip(1000)).Start(); // yes, fire and forget
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