using Microsoft.VisualBasic.ApplicationServices;
using System.Text.Json;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using ScreenTime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.Design;
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
        host.Start();


        Application.Run(ServiceProvider.GetRequiredService<HiddenForm>());

        host.StopAsync().Wait();

    }
    public static IServiceProvider? ServiceProvider { get; private set; }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService((sp) => sp.GetRequiredService<IScreenTimeStateClient>());
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
                    serviceProvider.GetRequiredService<UserStateProvider>())
                    };
                    return client;
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
                    sp.GetRequiredService<LockProvider>()
                    ));
            });


        // if not set, write to the registry to run this application on on startup
        if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", null) == null)
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", Environment.ProcessPath ?? String.Empty);

        return builder;
    }
}