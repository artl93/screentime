
using Microsoft.Extensions.Logging;
using ScreenTime;

internal class HiddenForm : Form
{
    private readonly NotifyIcon icon;

    public HiddenForm(IScreenTimeStateClient client, LockProvider lockProvider, ILogger? logger)
    {
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.ShowIcon = false;
        this.Visible = false;
        icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = new ContextMenuStrip()
        };
        icon.ContextMenuStrip.Items.Add("Reset", null, (s, e) => { client.Reset(); });
        icon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => { icon.Visible = false; Environment.Exit(0); });
        icon.Visible = true;
        icon.Text = "Connecting...";
        client.OnMessageUpdate += (s, e) =>
        {
            logger?.LogWarning($"Message update: {e.Message.Message}");
            ShowMessage(e.Message);
        } ;
        client.OnUserStatusChanged += (s, e) =>
        {
            logger?.LogWarning($"User status changed: {Enum.GetName(e.Status.State)} - {e.Status.LoggedInTime}");
            UpdateTooltip(e.Status);
            if (e.Status.State == UserState.Lock)
            {
                logger?.LogWarning("Locking workstation.");
                Task.Delay(10000);
                lockProvider.Lock();
            }
        };
        client.OnDayRollover += (s, e) =>
        {
            logger?.LogInformation($"Day rollover.{e.Message}");
            ShowMessage(e.Message);
        };
        client.OnTimeUpdate += (s, e) => UpdateTooltip(e.Status);

    }

    private void UpdateTooltip(UserStatus status)
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

    private void ShowMessage(UserMessage message)
    {
        icon.BalloonTipTitle = message.Title ?? string.Empty;
        icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
        Task.Run(() => icon.ShowBalloonTip(1000));
    }

}
