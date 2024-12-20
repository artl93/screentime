using Microsoft.VisualBasic.ApplicationServices;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using ScreenTime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;


var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<IScreenTimeStateClient>(serviceProvider =>
            {
                // create the server
                var firstArg = args.Length > 0 ? args[0] : string.Empty;
                IScreenTimeStateClient client = firstArg switch
                {
                    "develop" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://localhost:7186"),
                    "live" => new ScreenTimeServiceClient(serviceProvider.GetRequiredService<HttpClient>()).SetBaseAddress("https://screentime.azurewebsites.net"),
                    _ => new ScreenTimeLocalService(serviceProvider.GetRequiredService<TimeProvider>(), 
                        serviceProvider.GetRequiredService<UserConfigurationReader>().GetConfiguration(),
                        serviceProvider.GetRequiredService<UserStateProvider>())
                };
                return client;
            });
        services.AddSingleton<SystemEventHandlers>((sp) => new SystemEventHandlers(sp.GetRequiredService<IScreenTimeStateClient>()));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<NotificationClient>((sp) => new NotificationClient(sp.GetRequiredService<IScreenTimeStateClient>()));
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
    });


// if not set, write to the registry to run this application on on startup
if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", null) == null)
    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", Environment.ProcessPath ?? String.Empty);



var app = builder.Build();
app.Run();