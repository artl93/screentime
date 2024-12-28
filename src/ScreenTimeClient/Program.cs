using ScreenTime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application = System.Windows.Forms.Application;
using NetEscapades.Extensions.Logging.RollingFile;


static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DoRegistration(args);

        var host = CreateHostBuilder(args).Build();
        ServiceProvider = host.Services;
        // ordering is important here to hook the event handlers
        var form = ServiceProvider.GetRequiredService<HiddenForm>();
        var client = ServiceProvider.GetRequiredService<IScreenTimeStateClient>();
        host.Start();
        client.StartSession("start program");

        Application.ApplicationExit += (s, e) =>
        {
            client.EndSession("end program");
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
                services.AddHostedServices();
                services.AddScreenTimeClient(args);
                services.AddUserConfiguration();
                services.AddLoggingConfiguration();
                services.AddSingleton<SystemStateEventHandlers>();
                services.AddSingleton(TimeProvider.System);
                services.AddHttpClientConfiguration();
                services.AddSingleton<UserStateRegistryProvider>();
                services.AddSingleton<SystemLockStateService>();
                services.AddSingleton<HiddenForm>((sp) => new HiddenForm(
                    sp.GetRequiredService<IScreenTimeStateClient>(),
                    sp.GetRequiredService<SystemLockStateService>(),
                    sp.GetRequiredService<IUserConfigurationProvider>(),
                    sp.GetRequiredService<ILogger<HiddenForm>>()));
            });
            
         return builder;
    }

    static void DoRegistration(string[] args)
    {
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
        if (args.Contains("install_global"))
        {
            // install the application to run on startup
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", Environment.ProcessPath ?? String.Empty);
        }
        if (args.Contains("uninstall_global"))
        {
            // uninstall the application from running on startup
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run", "ScreenTime", String.Empty);
        }
    }
}
