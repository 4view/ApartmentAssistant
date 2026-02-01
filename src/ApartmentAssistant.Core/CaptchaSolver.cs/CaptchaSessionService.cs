namespace ApartmentAssistant.Core.CaptchaSolver;

public class CapthcaSessionService
{
    private readonly Dictionary<long, CaptchaSession> _session = new();

    private readonly ILogger<CapthcaSessionService> _logger;

    public CapthcaSessionService(ILogger<CapthcaSessionService> logger)
    {
        _logger = logger;
    }

    public void CreateSession(long chatId)
    {
        _session[chatId] = new CaptchaSession
        {
            UserId = chatId,
            Attempts = 0,
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
