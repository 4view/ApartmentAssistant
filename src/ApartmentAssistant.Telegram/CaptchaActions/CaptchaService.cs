namespace ApartmentAssistant.Telegram.CaptchaSolver;

public class CapthcaService
{
    private readonly SeleniumService _seleniumService;

    private readonly ITelegramBotClient _botClient;

    private readonly Dictionary<long, CaptchaSession> _session = new();

    private readonly ILogger<CapthcaService> _logger;

    public CapthcaService(
        SeleniumService seleniumService,
        ITelegramBotClient bot,
        ILogger<CapthcaService> logger
    )
    {
        _seleniumService = seleniumService;
        _botClient = bot;
        _logger = logger;
    }

    /// <summary>
    /// Получение и отправка изображение капчи
    /// </summary>
    /// <param name="isPageLoaded">Загружена ли страница в момент вызова метода</param>
    public async Task CaptureAndSendCaptchaAsync(long chatId, bool isPageLoaded)
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
    /// Процесс обработки ответа пользователя на капчу
    /// </summary>
    public async Task<bool> ProcessCaptchaAnswer(
        UserEntity user,
        long chatId,
        string answer,
        CaptchaSession session,
        CancellationToken cancellationToken
    )
    {
        var isPageLoaded = true;

        _logger.LogInformation(
            $"Начался процесс обработки капчи для пользователя {chatId}. Попытка {session.Attempts + 1}"
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
                    $"Пользователь {chatId} ввел неверную капчу. "
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

                RemoveCompletedSession(chatId);

                return false;
            }
        }
    }

    public void CreateSession(long chatId, TenementIndicationEntity indications)
    {
        _session[chatId] = new CaptchaSession
        {
            UserId = chatId,
            Attempts = 0,
            Indications = indications,
            ExpiryTime = DateTime.UtcNow.AddMinutes(3),
        };

        _logger.LogInformation($"Созданна сессия для пользователя {chatId}");
    }

    public CaptchaSession? GetSession(long chatId)
    {
        if (_session.TryGetValue(chatId, out var session))
        {
            if (session.IsExpired)
            {
                _session.Remove(chatId);
                return null;
            }

            return session;
        }
        return null;
    }

    public void RemoveCompletedSession(long chatId)
    {
        _session.Remove(chatId);
        _logger.LogInformation($"Сессия удалена для пользователя {chatId}");
    }
}
