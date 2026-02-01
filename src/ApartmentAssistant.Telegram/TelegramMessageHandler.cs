public class TelegramMessageHandler : BackgroundService
{
    private readonly ILogger<TelegramMessageHandler> _logger;

    private readonly ITelegramBotClient _botClient;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly SeleniumService _seleniumService;

    private readonly CapthcaSessionService _captchaService;

    //
    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        ITelegramBotClient bot,
        IServiceScopeFactory factory,
        SeleniumService seleniumService,
        CapthcaSessionService captchaService
    )
    {
        _scopeFactory = factory;
        _logger = logger;
        _botClient = bot;
        _seleniumService = seleniumService;
        _captchaService = captchaService;
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

            var result = await ProcessCaptchaAnswer(
                user,
                userId,
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
                await CaptureAndSendCaptchaAsync(chatId, isPageLoaded);

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

    private async Task CaptureAndSendCaptchaAsync(long chatId, bool isPageLoaded)
    {
        try
        {
            var imageBytes = await _seleniumService.CaptureCaptchaAsStreamAsync(isPageLoaded);

            if (imageBytes == null || imageBytes.Length == 0)
                throw new Exception("Не удалось получить изображение капчи");

            using var stream = new MemoryStream(imageBytes);

            await _botClient.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromStream(stream, "captcha.png"),
                caption: "Напишите текст с картинки:"
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }

    /// <summary>
    /// Обрабатываем ответ пользователя на отправленное изображение капчи
    /// </summary>
    /// <returns>Успешно ли прошла авторизация</returns>
    private async Task<bool> ProcessCaptchaAnswer(
        UserEntity user,
        long userId,
        long chatId,
        string answer,
        CaptchaSession session,
        CancellationToken cancellationToken
    )
    {
        var isPageLoaded = true;

        _logger.LogInformation(
            $"Начался процесс обработки капчи для пользователя {userId}. Попытка {session.Attempts + 1}"
        );

        if (string.IsNullOrEmpty(answer))
        {
            await _botClient.SendMessage(
                chatId,
                "Пожалуйста, введите текст с картинки",
                cancellationToken: cancellationToken
            );
            return false;
        }

        var isAuthorizationSuccessful = _seleniumService.Authorization(user, answer);

        if (isAuthorizationSuccessful)
        {
            await _botClient.SendMessage(
                chatId,
                "Авторизация прошла успешно.",
                cancellationToken: cancellationToken
            );

            _logger.LogInformation($"Пользователь {chatId} успешно авторизировался");
            return true;
        }
        else
        {
            session.Attempts++;

            if (session.Attempts < session.MaxAttempts)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Вы ввели неверную капчу: {answer}. \nОсталось {session.MaxAttempts - session.Attempts} попыток!",
                    cancellationToken: cancellationToken
                );

                await CaptureAndSendCaptchaAsync(chatId, isPageLoaded);

                _logger.LogInformation(
                    $"Пользователь {userId} ввел неверную капчу. "
                        + $"Осталось попыток: {session.MaxAttempts - session.Attempts}"
                );

                return false;
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Превышено максимальное количество попыток!",
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation(
                    $"Пользователь {chatId} превысил максимальное кол-во попыток"
                );

                _captchaService.RemoveCompletedSession(userId);

                return false;
            }
        }
    }
}
