public class TelegramMessageHandler : BackgroundService
{
    private readonly ILogger<TelegramMessageHandler> _logger;

    private readonly CaptchaProcessor _captchaProcessor;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ITelegramBotClient _botClient;

    private readonly CapthcaSessionService _captchaService;

    //
    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        ITelegramBotClient bot,
        IServiceScopeFactory factory,
        CapthcaSessionService captchaService,
        CaptchaProcessor captchaProcessor
    )
    {
        _scopeFactory = factory;
        _logger = logger;
        _botClient = bot;
        _captchaService = captchaService;
        _captchaProcessor = captchaProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var offset = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botClient.GetUpdates(
                    offset: offset,
                    timeout: 60,
                    cancellationToken: cancellationToken
                );

                foreach (var update in updates)
                {
                    await HandleUpdateAsync(update, cancellationToken);
                    offset = update.Id + 1;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Получен сигнал завершения работы...");
                break;
            }
        }
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: string text, From: { } fromUser } message)
            return;

        var chatId = message.Chat.Id;
        var userId = fromUser.Id;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Set<UserEntity>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            _logger.LogInformation($"Пользователь {user} не был найден!");
            return;
        }

        var session = _captchaService.GetSession(userId);

        // Если есть сессия с пользователем то ждем ответа на капчу
        if (session != null && !session.IsExpired)
        {
            _logger.LogInformation($"Обработка ответа на капчу для пользователя {session.UserId}");

            var result = await _captchaProcessor.ProcessCaptchaAnswer(
                user,
                chatId,
                message.Text,
                session,
                cancellationToken
            );

            if (result)
            {
                _captchaService.RemoveCompletedSession(userId);
            }

            return;
        }

        try
        {
            if (text == "/start")
            {
                var isPageLoaded = false;
                //Пользователь написал /Start и мы отправили ему изображение капчи
                await _captchaProcessor.CaptureAndSendCaptchaAsync(chatId, isPageLoaded);

                // Добавили пользователя в сессию по обработке капчи
                _captchaService.CreateSession(userId);
            }
            else { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки сообщения");
        }
    }
}
