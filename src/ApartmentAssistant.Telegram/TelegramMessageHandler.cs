public class TelegramMessageHandler : BackgroundService
{
    private readonly ILogger<TelegramMessageHandler> _logger;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ITelegramBotClient _botClient;

    private readonly CapthcaService _captchaService;

    private readonly SeleniumService _seleniumService;

    //
    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        ITelegramBotClient bot,
        IServiceScopeFactory factory,
        CapthcaService captchaService,
        SeleniumService seleniumService
    )
    {
        _scopeFactory = factory;
        _logger = logger;
        _botClient = bot;
        _captchaService = captchaService;
        _seleniumService = seleniumService;
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

            var result = await _captchaService.ProcessCaptchaAnswer(
                user,
                chatId,
                message.Text,
                session,
                cancellationToken
            );

            _seleniumService.InputTenementIndications(session.Indications);

            if (result)
            {
                _captchaService.RemoveCompletedSession(userId);
            }

            return;
        }

        try
        {
            if (text == "/start") { }
            else if (text.Contains("@TenementBot"))
            {
                var indications = ParseUserIndications(text, userId);

                if (indications != null)
                {
                    var isPageLoaded = false;
                    await _captchaService.CaptureAndSendCaptchaAsync(chatId, isPageLoaded);

                    _captchaService.CreateSession(userId, indications);
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId,
                        $"Некоторые поля были заполнены некорректно",
                        cancellationToken: cancellationToken
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки сообщения");
        }
    }

    public TenementIndicationEntity? ParseUserIndications(string text, long userId)
    {
        text = text.Replace("@TenementBot", "").Trim();

        var result = new TenementIndicationEntity()
        {
            UserId = userId,
            ContributionDate = DateTimeOffset.Now,
        };

        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        var currentSection = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (
                trimmedLine.StartsWith("Кухня", StringComparison.OrdinalIgnoreCase)
                || trimmedLine.StartsWith("Кухня:", StringComparison.OrdinalIgnoreCase)
            )
            {
                currentSection = "Кухня";
                continue;
            }

            if (
                trimmedLine.StartsWith("Ванная", StringComparison.OrdinalIgnoreCase)
                || trimmedLine.StartsWith("Ванная:", StringComparison.OrdinalIgnoreCase)
            )
            {
                currentSection = "Ванная";
                continue;
            }

            if (trimmedLine.Contains("=") && !string.IsNullOrEmpty(currentSection))
            {
                var parts = trimmedLine.Split("=");

                if (parts.Length == 2)
                {
                    var key = parts[0].Trim().ToLower();
                    var valueStr = parts[1].Trim();

                    if (
                        decimal.TryParse(
                            valueStr.Replace(".", ","),
                            NumberStyles.Any,
                            CultureInfo.InstalledUICulture,
                            out var value
                        )
                    )
                    {
                        if (key.Contains("горячая") && currentSection == "Кухня")
                            result.KitchenHotWater = value;
                        if (key.Contains("холодная") && currentSection == "Кухня")
                            result.KitchenColdWater = value;
                        if (key.Contains("горячая") && currentSection == "Ванная")
                            result.BathroomHotWater = value;
                        if (key.Contains("холодная") && currentSection == "Ванная")
                            result.BathroomColdWater = value;
                    }
                }
            }
        }

        if (
            result.BathroomColdWater == 0
            || result.BathroomHotWater == 0
            || result.KitchenColdWater == 0
            || result.KitchenHotWater == 0
        )
        {
            return null;
        }

        return result;
    }
}
