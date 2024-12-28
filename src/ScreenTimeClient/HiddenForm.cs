
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ScreenTime;

internal class HiddenForm : Form
{
    private readonly NotifyIcon icon;
    private readonly ILogger? logger;
    private readonly IUserConfigurationProvider _userConfigurationProvider;
    private bool messageIsVisible = false;
    // private bool silentMode = false;
    private bool _disableLock;
    private int _lockDelaySeconds;

    public HiddenForm(IScreenTimeStateClient client, 
        SystemLockStateService lockProvider, 
        IUserConfigurationProvider userConfigurationProvider, 
        ILogger? logger)
    {
        this.logger = logger;
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
        SystemEvents.SessionSwitch += (s, e) =>
        {

            switch (e.Reason)
            {
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.ConsoleConnect:
                    logger?.LogInformation("Session unlocking for {Reason}.", Enum.GetName(e.Reason));
                    isLocked = false;
                    break;
            }
        };

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
            if (e.Status.State == UserState.Lock)
            {
                HandleLocking(lockProvider);
            }
        };
        client.EventHandlerEnsureComputerState += (s, e) =>
        {
            if (e.State == UserState.Lock)
            {
                HandleLocking(lockProvider);
            }
        };
        client.OnDayRollover += (s, e) =>
        {
            logger?.LogInformation("Day rollover. {Message}", e.Message);
            ShowMessage(e.Message);
        };
        client.OnTimeUpdate += (s, e) => UpdateTooltip(e.Status);

    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        throw new NotImplementedException();
    }

    private readonly Lock messageBoxLock = new();
    private readonly Lock screenLockLock = new();
    private bool isLocked = false;
    private Task MessageBoxTask = Task.CompletedTask;

    private void HandleLocking(SystemLockStateService lockProvider)
    {
        int delay = Math.Max(_lockDelaySeconds, 5); // avoid pathological cases
                                                    // display modeless message box that says "Locking"
        ShowModelessMessageBox(delay);
        EnsureLock(lockProvider, delay);
    }

    private void EnsureLock(SystemLockStateService lockProvider, int delay)
    {
        if (_disableLock)
            return;
        if (isLocked)
            return;
        lock (screenLockLock)
        {
            if (isLocked)
                return;
            isLocked = true;
            logger?.BeginScope("Start locking task");
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(delay));
                logger?.LogInformation("Locking...");
                lockProvider.Lock();
                // is locked will need to be cleared by a signal from SystemStateEventHandler
                logger?.LogInformation("Locked.");
            });
        }
    }

    private void ShowModelessMessageBox(int delay)
    {
        if (messageIsVisible)
            return;
        lock (messageBoxLock)
        {
            if (messageIsVisible)
                return;
            messageIsVisible = true;
            if (!MessageBoxTask.IsCompleted)
                return;
            MessageBoxTask = Task.Run(() =>
            {
                ShowLockingMessage(_disableLock, delay);
                messageIsVisible = false;
                Task.Delay(TimeSpan.FromSeconds(delay)).Wait();
            });
        }
    }

    private void ShowLockingMessage(bool disableLock, int delay)
    {
        if (disableLock)
        {
            logger?.LogWarning("ShowMessage: Locking is disabled.");
            MessageBox.Show("Locking is disabled.", "Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            logger?.LogWarning("ShowMessage: Locking in {Delay} seconds...", delay);
            MessageBox.Show($"Locking in {delay} seconds...", "Screen Time", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

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
        // if (!silentMode)
        {
            icon.BalloonTipTitle = message.Title ?? string.Empty;
            icon.BalloonTipText = (message.Icon ?? string.Empty) + " " + (message.Message ?? string.Empty);
            Task.Run(() => icon.ShowBalloonTip(1000));
        }
    }
}
