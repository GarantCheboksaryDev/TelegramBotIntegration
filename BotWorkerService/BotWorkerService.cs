using CustomJobHandler;
using Sungero.Logging;
using TelegramBot;

public sealed class BotService : BackgroundService
{
    private static ILog _logger => Logs.GetLogger<BotService>();

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Debug("BotService started at {time}", DateTimeOffset.Now);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BotWrapper.Start(_logger);
    }
}