
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using ScreenTime.Common;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScreenTimeClient
{
    public class RemoteUserStateProvider(IHttpClientFactory httpClientFactory, ILogger<RemoteUserStateProvider> logger) : IDisposable
    {
        private const string cacheFileExtension = ".msalcache.bin";
        private readonly HttpClient httpClient = httpClientFactory.CreateClient("shared");
        private readonly ILogger logger = logger;
        private IPublicClientApplication? publicClientApp;
        const string configUrl = "/configuration";
        const string extensionUrl = "/extensions/request";
        const string profileUrl = "/profile/";
        const string heartbeatUrl = "heartbeat";

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
;
                if (result != null)
                {
                    logger?.LogInformation("Login result: {Result}", result);
                    var token = result.AccessToken;
                    if (result != null && !string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        IsLoggedIn = true;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Login error: {Message}", e.Message);
            }
            return false;

        }

        public async Task LogoutAsync()
        {
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

        internal async Task<UserConfiguration> GetConfigurationAsync()
        {
            var response = await httpClient.GetAsync(configUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserConfiguration>(content, options) ?? new UserConfiguration("default");

        }

        internal async Task SendHeartbeatAsync(Heartbeat heartbeat)
        {
            try
            {
                var json = JsonSerializer.Serialize(heartbeat, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync(heartbeatUrl, content);
                response.EnsureSuccessStatusCode();
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
    }
}