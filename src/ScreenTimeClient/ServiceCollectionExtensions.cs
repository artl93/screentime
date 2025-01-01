﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using ScreenTimeClient.Configuration;

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

        public static IServiceCollection AddScreenTimeClient(this IServiceCollection services)
        {
            services.AddSingleton(serviceProvider =>
            {
                IScreenTimeStateClient client = 
                    new ScreenTimeLocalService(serviceProvider.GetRequiredService<TimeProvider>(),
                        serviceProvider.GetRequiredService<IUserConfigurationProvider>(),
                        serviceProvider.GetRequiredService<UserStateRegistryProvider>(),
                        serviceProvider.GetRequiredService<IIdleTimeDetector>(),
                        serviceProvider.GetRequiredService<ILogger<ScreenTimeLocalService>>());
                return client;
            });
            return services;
        }

        public static IServiceCollection AddUserConfiguration(this IServiceCollection services, string[] args)
        {
            services.AddSingleton((sp) =>
                new LocalUserConfigurationProvider(
                    sp.GetRequiredService<IUserConfigurationReader>(),
                    sp.GetRequiredService<TimeProvider>()));

            services.AddSingleton<RemoteUserConfigurationProvider>();

            services.AddSingleton((sp) =>
            {
                return new SwitchableUserConfigurationProvider(
                    sp.GetRequiredService<RemoteUserConfigurationProvider>(),
                    sp.GetRequiredService<LocalUserConfigurationProvider>());
            });


            services.AddSingleton<IUserConfigurationProvider>((sp) =>
            {
                if (args.Contains("develop") || args.Contains("live"))
                    return sp.GetRequiredService<SwitchableUserConfigurationProvider>();
                return sp.GetRequiredService<LocalUserConfigurationProvider>();
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

        public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services, string[] args)
        {
            services.AddHttpClient("shared", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(args.Contains("develop") ? 10 : 20);
                client.BaseAddress = args.Contains("develop") ? new Uri("https://localhost:7115") : new Uri("https://screentime.azurewebsites.net");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "ScreenTime");
            });
            return services;
        }
    }
}
