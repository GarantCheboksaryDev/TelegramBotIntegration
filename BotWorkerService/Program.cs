using Sungero.Logging;

class Program
{
    static void Main(string[] args)
    {
        Logs.Ņonfiguration.Configure();
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<BotService>();
        });
}