using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ScreenTimeClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService((sp) => sp.GetRequiredService<IScreenTimeStateClient>())
                .ActivateSingleton<SystemLockStateService>()
                .ActivateSingleton<SystemStateEventHandlers>();
            return services;
        }

        public static IServiceCollection AddScreenTimeClient(this IServiceCollection services, string[] args)
        {
            services.AddSingleton(serviceProvider =>
            {
                var firstArg = args.Length > 0 ? args[0] : string.Empty;
                IScreenTimeStateClient client = firstArg switch
                {
                    "develop" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://localhost:7186"),
                    "live" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://screentime.azurewebsites.net"),
                    _ => new ScreenTimeLocalService(serviceProvider.GetRequiredService<TimeProvider>(),
                        serviceProvider.GetRequiredService<IUserConfigurationProvider>(),
                        serviceProvider.GetRequiredService<UserStateRegistryProvider>(),
                        serviceProvider.GetRequiredService<IIdleTimeDetector>(),
                        serviceProvider.GetRequiredService<ILogger<ScreenTimeLocalService>>())
                };
                return client;
            });
            return services;
        }

        public static IServiceCollection AddUserConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IUserConfigurationProvider, UserConfigurationProvider>((sp) =>
            {
                return new UserConfigurationProvider(
                    sp.GetRequiredService<IUserConfigurationReader>(),
                    sp.GetRequiredService<TimeProvider>());
            });
            services.AddSingleton<IUserConfigurationReader, UserConfigurationRegistryReader>();
            return services;
        }

        public static IServiceCollection AddLoggingConfiguration(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddFile(options =>
                {
                    options.FileName = "screentime-";
                    options.LogDirectory = Path.GetTempPath();
                    options.FileSizeLimit = 20 * 1024 * 1024;
                    options.FilesPerPeriodicityLimit = 200;
                    options.Extension = "log";
                });
            });
            return services;
        }

        public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services)
        {
            services.AddHttpClient("screentimeClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "ScreenTime");
            })
            .AddStandardResilienceHandler();
            return services;
        }

    }
}
