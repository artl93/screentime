
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Win32;
using ScreenTimeClient;
using System.Diagnostics;
using System.Net.Http.Headers;

internal class HiddenForm : Form
{
    private readonly NotifyIcon icon;
    private readonly HttpClient httpClient;
    private readonly ToolStripItem? usernameItem;
    private readonly ILogger? logger;
    private readonly IUserConfigurationProvider _userConfigurationProvider;
    private bool messageIsVisible = false;
    // private bool silentMode = false;
    private bool _disableLock;
    private bool _enableOnline;
    private int _lockDelaySeconds;
    private readonly List<ToolStripItem> preLoginItemsList = [];
    private readonly List<ToolStripItem> postLoginItemsList = [];
    private readonly Lock messageBoxLock = new();
    private readonly Lock screenLockLock = new();
    private bool isLocked = false;
    private Task MessageBoxTask = Task.CompletedTask;
    private IPublicClientApplication? _publicClientApp;

    public HiddenForm(IScreenTimeStateClient client,
        SystemLockStateService lockProvider,
        IUserConfigurationProvider userConfigurationProvider,
        HttpClient httpClient,
        ILogger? logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        _userConfigurationProvider = userConfigurationProvider;
        var result = _userConfigurationProvider.GetUserConfigurationForDayAsync().Result;
        _disableLock = result.DisableLock;
        _enableOnline = result.EnableOnline;
        _lockDelaySeconds = result.DelayLockSeconds;
        _userConfigurationProvider.OnConfigurationChanged += (s, e) =>
        {
            _disableLock = e.Configuration.DisableLock;
            _lockDelaySeconds = e.Configuration.DelayLockSeconds;
            _enableOnline = e.Configuration.EnableOnline;
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

        if (!_enableOnline)
        {
            logger?.LogInformation("Online mode disabled.");
            icon.ContextMenuStrip.Items.Add("Request 5 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(5); });
            icon.ContextMenuStrip.Items.Add("Request 15 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(15); });
            icon.ContextMenuStrip.Items.Add("Request 60 minute extension", null, async (s, e) => { await client.RequestExtensionAsync(60); });
        }
        else
        {
            logger?.LogInformation("Online mode enabled.");
            usernameItem = icon.ContextMenuStrip.Items.Add("Username", null);
            usernameItem.Visible = false;
            preLoginItemsList.Add(icon.ContextMenuStrip.Items.Add("Login...", null, async (s, e) => { await DoLoginAsync(); }));
            postLoginItemsList.Add(icon.ContextMenuStrip.Items.Add("Request 5 minute extension", null, async (s, e) => { await RequestExtensionAsync(5); }));
            postLoginItemsList.Add(icon.ContextMenuStrip.Items.Add("Request 15 minute extension", null, async (s, e) => { await RequestExtensionAsync(15); }));
            postLoginItemsList.Add(icon.ContextMenuStrip.Items.Add("Request 60 minute extension", null, async (s, e) => { await RequestExtensionAsync(60); }));
            postLoginItemsList.Add(icon.ContextMenuStrip.Items.Add("Logout...", null, (s, e) => { DoLogout(); }));
        }

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
        };
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

        if (_enableOnline)
        {
            var account = GetClientApp().GetAccountsAsync().GetAwaiter().GetResult().FirstOrDefault();
            if (account != null)
            {
                UpdateForLogin(account, "");
            }
            else
            {
                UpdateForLogout();
            }
        }
    }

    private async Task RequestExtensionAsync(int v)
    {
         var result = await this.httpClient.PutAsync($"https://localhost:7115/request/{v}", null);
    }

    private void UpdateForLogout()
    {
        preLoginItemsList.ForEach(i => i.Visible = true);
        postLoginItemsList.ForEach(i => i.Visible = false);
        ShowMessage(new UserMessage("Not logged in", "You are not logged in.", "🔓", "Okay"));
        if (usernameItem != null)
            usernameItem.Visible = false;
    }

    private void UpdateForLogin(IAccount account, string token)
    {
        preLoginItemsList.ForEach(i => i.Visible = false);
        postLoginItemsList.ForEach(i => i.Visible = true);
        ShowMessage(new UserMessage("Logged in", $"You are logged in as {account.Username}.", "🔒", "Okay"));
        if (usernameItem != null)
        {
            usernameItem.Visible = true;
            usernameItem.Text = $"Signed in: ({account.Username}) 🔒";
        }
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var result = httpClient.GetAsync("https://localhost:7115/request/5").GetAwaiter().GetResult();
        result = httpClient.PutAsync("https://localhost:7115/request/5", null).GetAwaiter().GetResult();
    }

    private IPublicClientApplication GetClientApp()
    {
        if (_publicClientApp == null)
        {
            _publicClientApp = PublicClientApplicationBuilder
                // .Create("b1982a95-6b93-46ca-844c-f0594227e2d7")
                .Create("4eb97520-4902-4817-ab35-ae38739253ba")
                .WithClientId("b1982a95-6b93-46ca-844c-f0594227e2d7")
                .WithAuthority("https://login.microsoftonline.com/4eb97520-4902-4817-ab35-ae38739253ba/")
                .WithDefaultRedirectUri()
                .WithClientName("ScreenTime taskbar client")
                .WithClientVersion(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                .Build();
            MsalCacheHelper cacheHelper = CreateCacheHelperAsync().GetAwaiter().GetResult();

            // Let the cache helper handle MSAL's cache, otherwise the user will be prompted to sign-in every time.
            cacheHelper.RegisterCache(_publicClientApp.UserTokenCache);
        }
        return _publicClientApp;
    }

    private async Task DoLoginAsync()
    {

        var app = GetClientApp();

        var accounts = await app.GetAccountsAsync();
        var scopes = new string[] { "user.read", "api://b1982a95-6b93-46ca-844c-f0594227e2d7/access_as_user" };
        AuthenticationResult result;

        try
        {
            if (accounts.Any())
                result = app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                  .ExecuteAsync().GetAwaiter().GetResult();
            else 
                result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();

            logger?.LogInformation("Login result: {Result}", result);
            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                UpdateForLogin(result.Account, result.AccessToken);
            }
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Login error: {Message}", e.Message);
        }


    }

    private void DoLogout()
    {
        var app = GetClientApp();
        var accounts = app.GetAccountsAsync().GetAwaiter().GetResult();
        foreach (var account in accounts)
        {
            app.RemoveAsync(account).GetAwaiter().GetResult();
        }
        UpdateForLogout();
    }

    private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
    {
        var storageProperties = new StorageCreationPropertiesBuilder(
                          System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".msalcache.bin",
                          MsalCacheHelper.UserRootDirectory)
                            .Build();

        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(
                    storageProperties,
                    new TraceSource("MSAL.CacheTrace"))
                 .ConfigureAwait(false);

        return cacheHelper;
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        throw new NotImplementedException();
    }



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
