namespace ApartmentAssistant.Telegram.CaptchaActions;

public class CaptchaProcessor
{
    private readonly SeleniumService _seleniumService;

    private readonly ITelegramBotClient _botClient;

    private readonly ILogger<CaptchaProcessor> _logger;

    private readonly CapthcaSessionService _captchaService;

    public CaptchaProcessor(
        SeleniumService seleniumService,
        ITelegramBotClient botClient,
        ILogger<CaptchaProcessor> logger,
        CapthcaSessionService captchaService
    )
    {
        _seleniumService = seleniumService;
        _botClient = botClient;
        _logger = logger;
        _captchaService = captchaService;
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
    /// Обрабатываем ответ пользователя на отправленное изображение капчи
    /// </summary>
    /// <returns>Успешно ли прошла авторизация</returns>
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

                _captchaService.RemoveCompletedSession(chatId);

                return false;
            }
        }
    }
}
