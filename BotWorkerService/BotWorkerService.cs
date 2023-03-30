using TelegramBot;

public sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;

    public BotService(ILogger<BotService> logger) =>
        _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Application starting");
            BotProgram.Start(_logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }
}