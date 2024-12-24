
using Microsoft.Extensions.Logging;
using ScreenTime;

internal class HiddenForm : Form
{
    private readonly NotifyIcon icon;
    private readonly IUserConfigurationProvider _userConfigurationProvider;
    private bool messageIsVisible = false;
    private bool silentMode = false;
    private bool _disableLock;
    private int _lockDelaySeconds; 

    public HiddenForm(IScreenTimeStateClient client, 
        SystemLockStateService lockProvider, 
        IUserConfigurationProvider userConfigurationProvider, 
        ILogger? logger)
    {
        _userConfigurationProvider = userConfigurationProvider;
        var result = _userConfigurationProvider.GetUserConfigurationForDayAsync().Result;
        _disableLock = result.DisableLock;
        _lockDelaySeconds = result.DelayLockSeconds;
        _userConfigurationProvider.OnConfigurationChanged += (s, e) =>
        {
            _disableLock = e.Configuration.DisableLock;
            _lockDelaySeconds = e.Configuration.DelayLockSeconds;
            logger?.LogInformation("Lock {State} by configuration. Delay {Seconds}.", _disableLock ? "disabled" : "enabled", _lockDelaySeconds);
        };
        logger?.LogInformation("Lock {State} by configuration. Delay {Seconds}", _disableLock ? "disabled" : "enabled", _lockDelaySeconds);

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
            logger?.LogWarning("Message update: {Message}", e.Message.Message);
            ShowMessage(e.Message);
        } ;
        client.OnUserStatusChanged += (s, e) =>
        {
            logger?.LogWarning("User status changed: {State} - {LoggedInTime}", Enum.GetName(e.Status.State), e.Status.LoggedInTime);
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
                            logger?.LogWarning("Ensure computer state: {State}", Enum.GetName(e.State));
                            if (!_disableLock)
                            {
                                MessageBox.Show($"Time is up. Locking this machine in {_lockDelaySeconds} seconds.");
                                // leave this safety delay in place to prevent the lock from happening too quickly
                                Task.Delay(TimeSpan.FromSeconds(Math.Max(_lockDelaySeconds, 2))).Wait();
                                lockProvider.Lock();
                            }
                            else
                            {
                                MessageBox.Show($"Imagine this computer is locked or I will REALLY lock it in the future.");
                            }
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
            logger?.LogInformation("Day rollover. {Message}", e.Message);
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
            UserState.Lock => SystemIcons.Shield,
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
