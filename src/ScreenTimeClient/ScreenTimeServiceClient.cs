using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using ScreenTime.Common;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace ScreenTimeClient
{
    public class UserMessageEventArgs(object Sender, UserMessage Message) : EventArgs 
    {
        public UserMessage Message { get; } = Message;
        public object Sender { get; } = Sender;
    }

    public class ScreenTimeServiceClient : IDisposable
    {
        private const string cacheFileExtension = ".msalcache.bin";
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private IPublicClientApplication? publicClientApp;
        const string configUrl = "/configuration";
        const string extensionUrl = "/extensions/request";
        const string profileUrl = "/profile/";
        const string heartbeatUrl = "heartbeat";
        private readonly HubConnection connection;
        public event EventHandler<UserMessageEventArgs>? OnMessage;

        public ScreenTimeServiceClient(IHttpClientFactory httpClientFactory, HubConnection connection, ILogger<ScreenTimeServiceClient> logger)
        {
            this.httpClient = httpClientFactory.CreateClient("shared");
            this.connection = connection;
            this.logger = logger;
            connection.Closed += error => Task.CompletedTask;

            connection.Reconnecting += error =>
            {
                Debug.Assert(connection.State == HubConnectionState.Reconnecting);

                // Notify users the connection was lost and the client is reconnecting.
                // Start queuing or dropping messages.

                logger?.LogInformation(error?.Message);

                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                Debug.Assert(connection.State == HubConnectionState.Connected);

                // Notify users the connection was reestablished.
                // Start dequeuing messages queued while reconnecting if any.

                return Task.CompletedTask;
            };
            connection.On<UserMessage>("Message", (message) =>
            {
                OnMessage?.Invoke(this, new UserMessageEventArgs(this, message));
            });
        }

        public static async Task<bool> ConnectWithRetryAsync(HubConnection connection, CancellationToken token)
        {
            // Keep trying to until we can start or the token is canceled.
            while (true)
            {
                try
                {
                    await connection.StartAsync(token);
                    Debug.Assert(connection.State == HubConnectionState.Connected);
                    return true;
                }
                catch when (token.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    // Failed to connect, trying again in 5000 ms.
                    Debug.Assert(connection.State == HubConnectionState.Disconnected);
                    await Task.Delay(5000);
                }
            }
        }



        private readonly JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public bool IsLoggedIn { get; private set; } = false;

        public async Task<bool> LoginAsync(bool silent = false)
        {

            var app = GetClientApp();

            var accounts = await app.GetAccountsAsync();
            var scopes = new string[] { "user.read", "api://b1982a95-6b93-46ca-844c-f0594227e2d7/access_as_user" };
            AuthenticationResult? result = null;

            try
            {
                if (accounts.Any())
                    result = app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync().Result;
                else if (!silent)
                    result = app.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
                if (result != null)
                {
                    logger?.LogInformation("Login result: {Result}", result);
                    var token = result.AccessToken;
                    if (result != null && !string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        if (connection.State != HubConnectionState.Connected)
                        {
                            connection.StartAsync(CancellationToken.None).RunSynchronously();
                        }
                        IsLoggedIn = true;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                IsLoggedIn = false;
                await LogoutAsync();
                logger?.LogError(e, "Login error: {Message}", e.Message);
            }
            // await ConnectWithRetryAsync(connection, CancellationToken.None);

            return false;

        }

        public async Task SendExtensionRequestAsync(int minutes)
        {
            var request = new ExtensionRequest(TimeSpan.FromMinutes(minutes));
            await RequestExtensionAsync(request);
        }

        public async Task LogoutAsync()
        {
            if (!IsLoggedIn)
                return;
            if (connection.State == HubConnectionState.Connected)
                await connection.StopAsync();

            var app = GetClientApp();
            var accounts = await app.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await app.RemoveAsync(account);
            }
            IsLoggedIn = false;

            httpClient.DefaultRequestHeaders.Authorization = null;  
        }


        private IPublicClientApplication GetClientApp()
        {
            if (publicClientApp == null)
            {
                publicClientApp = PublicClientApplicationBuilder
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
                cacheHelper.RegisterCache(publicClientApp.UserTokenCache);
            }
            return publicClientApp;
        }

        private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                              System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + cacheFileExtension,
                              MsalCacheHelper.UserRootDirectory)
                                .Build();

            MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(
                        storageProperties,
                        new TraceSource("MSAL.CacheTrace"))
                     .ConfigureAwait(false);

            return cacheHelper;
        }

        public async Task<string> GetUsernameAsync()
        {
            var app = GetClientApp();
            var accounts = await app.GetAccountsAsync();
            if (accounts.Any())
            {
                return accounts.First().Username;
            }
            return "(Invalid username)";
        }

        // Updated to use SignalR hub call.
        internal async Task<DailyConfiguration> GetConfigurationAsync()
        {
            return await connection.InvokeAsync<DailyConfiguration>("GetConfiguration");
        }

        // Updated to use SignalR hub call.
        internal async Task SendHeartbeatAsync(Heartbeat heartbeat)
        {
            try
            {
                await connection.InvokeAsync("SendHeartbeat", heartbeat);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

        public void Dispose()
        {
            ((IDisposable)httpClient).Dispose();
        }

        // Updated to use SignalR hub call.
        internal async Task RequestExtensionAsync(ExtensionRequest request)
        {
            await connection.InvokeAsync("RequestExtension", request);
        }

        internal Task<string?> GetAccessTokenAsync()
        {
            return Task.FromResult(httpClient.DefaultRequestHeaders.Authorization?.Parameter);
        }
    }
}