public class TelegramNotificator : BackgroundService
{
    private readonly NotificationService _notificationService;

    private readonly ILogger<TelegramNotificator> _logger;

    private readonly ITelegramBotClient _botClient;

    public TelegramNotificator(
        NotificationService notification,
        ITelegramBotClient botClient,
        ILogger<TelegramNotificator> logger
    )
    {
        _notificationService = notification;
        _botClient = botClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_notificationService.IsNotificationPeriodStart())
            {
                try
                {
                    await _notificationService.CheckAndNotifyAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Получен сигнал завершения работы...");
                    break;
                }
            }
            await Task.Delay(2000);
        }
    }
}
