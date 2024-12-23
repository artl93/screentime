
using Microsoft.Extensions.Logging;
using ScreenTime;

internal class HiddenForm : Form
{
    private readonly NotifyIcon icon;
    private bool messageIsVisible = false;
    private bool silentMode = false;

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
        // icon.ContextMenuStrip.Items.Add("Reset", null, (s, e) => { client.Reset(); });
        icon.ContextMenuStrip.Items.Add("Request 5 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(5); });
        icon.ContextMenuStrip.Items.Add("Request 15 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(15); });
        icon.ContextMenuStrip.Items.Add("Request 60 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(60); });
        //icon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => 
        //{
        //    logger?.LogCritical($"Silent mode enabled by: {Environment.UserName} because they hit \"Exit\"");
        //    icon.Visible = false;
        //    silentMode = true; 
        //});
        // icon.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) Application.Exit(); };
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
        };
        client.EventHandlerEnsureComputerState += (s, e) =>
        {
            if (silentMode) 
                return;
            if (e.State == UserState.Lock)
            {
                if (!messageIsVisible)
                {
                    lock (this)
                    {
                        if (!messageIsVisible)
                        {
                            messageIsVisible = true;
                            logger?.LogWarning($"Ensure computer state: {Enum.GetName(e.State)}");
                                MessageBox.Show($"Imagine this computer is locked or I will REALLY lock it in the future.");
                                Task.Delay(5000).Wait();
                            messageIsVisible = false;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
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
        var extensionTime = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.ExtensionTime, minUnit: Humanizer.Localisation.TimeUnit.Second, maxUnit: Humanizer.Localisation.TimeUnit.Hour, precision: 2);
        var humanizedUptime = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.LoggedInTime, minUnit: Humanizer.Localisation.TimeUnit.Second, maxUnit: Humanizer.Localisation.TimeUnit.Hour, precision: 2);
        var humanizedDailyLimit = Humanizer.TimeSpanHumanizeExtensions.Humanize(status.DailyTimeLimit, minUnit: Humanizer.Localisation.TimeUnit.Minute, maxUnit: Humanizer.Localisation.TimeUnit.Hour, precision: 2);

        icon.Text = status.ExtensionTime == TimeSpan.Zero ?
            $"{status.Icon} Interactive time: {humanizedUptime} out of {humanizedDailyLimit}." :
            $"{status.Icon} Interactive time: {humanizedUptime} out of {humanizedDailyLimit} (Extended: {extensionTime})."; 

        icon.Icon = status.State switch
        {
            UserState.Okay => SystemIcons.Information,
            UserState.Warn => SystemIcons.Warning,
            UserState.Error => SystemIcons.Error,
            UserState.Lock => SystemIcons.GetStockIcon(StockIconId.Lock, StockIconOptions.SmallIcon),
            UserState.Paused => SystemIcons.GetStockIcon(StockIconId.World),
            _ => SystemIcons.Application
        };
    }

    private void ShowMessage(UserMessage message)
    {
        if (!silentMode)
        {
            icon.BalloonTipTitle = message.Title ?? string.Empty;
            icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
            Task.Run(() => icon.ShowBalloonTip(1000));
        }
    }
}
