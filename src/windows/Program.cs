﻿using ScreenTime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application = System.Windows.Forms.Application;


static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder(args).Build();
        ServiceProvider = host.Services;
        // ordering is important here to hook the event handlers
        var form = ServiceProvider.GetRequiredService<HiddenForm>();
        var client = ServiceProvider.GetRequiredService<IScreenTimeStateClient>();
        client.StartSessionAsync("start program");
        host.Start();

        Application.ApplicationExit += (s, e) =>
        {
            client.EndSessionAsync("end program");
            host.StopAsync().Wait();
        };
        Application.Run(form);

     }

    public static IServiceProvider? ServiceProvider { get; private set; }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService((sp) => sp.GetRequiredService<IScreenTimeStateClient>())
                    .ActivateSingleton<LockProvider>()
                    .ActivateSingleton<SystemEventHandlers>();
                services.AddSingleton(serviceProvider =>
                {
                    // create the server
                    var firstArg = args.Length > 0 ? args[0] : string.Empty;
                    IScreenTimeStateClient client = firstArg switch
                    {
                        "develop" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://localhost:7186"),
                        "live" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://screentime.azurewebsites.net"),
                        _ => new ScreenTimeLocalService(serviceProvider.GetRequiredService<TimeProvider>(),
                            serviceProvider.GetRequiredService<UserConfigurationReader>().GetConfiguration(),
                            serviceProvider.GetRequiredService<UserStateProvider>(), 
                            serviceProvider.GetRequiredService<ILogger<ScreenTimeLocalService>>())
                    };
                    return client;
                });
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    builder.AddFile("screentime.");
                });
                services.AddSingleton((sp) => new SystemEventHandlers(sp.GetRequiredService<IScreenTimeStateClient>()));
                services.AddSingleton(TimeProvider.System);
                services.AddHttpClient("screentimeClient", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("User-Agent", "ScreenTime");
                })
                    .AddStandardResilienceHandler();
                services.AddSingleton<UserStateProvider>();
                services.AddSingleton<LockProvider>();
                services.AddSingleton<UserConfigurationReader>();
                services.AddSingleton((sp) => new HiddenForm(
                    sp.GetRequiredService<IScreenTimeStateClient>(), 
                    sp.GetRequiredService<LockProvider>(),
                    sp.GetRequiredService<ILogger<HiddenForm>>()
                    ));
    });

        if (args.Contains("install"))
        {
            // install the application to run on startup
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", Environment.ProcessPath ?? String.Empty);
        }
        if (args.Contains("uninstall"))
        {
            // uninstall the application from running on startup
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", String.Empty);
        }

        return builder;
    }
}