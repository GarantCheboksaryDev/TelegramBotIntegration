using NLog.Extensions.Logging;
using System.Runtime.InteropServices;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<BotService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddNLog();
    });


if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.UseWindowsService();
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    builder.UseSystemd();

var host = builder.Build();
host.Run();