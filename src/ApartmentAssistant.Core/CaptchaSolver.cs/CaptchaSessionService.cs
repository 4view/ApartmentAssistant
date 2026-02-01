namespace ApartmentAssistant.Core.CaptchaSolver;

public class CapthcaSessionService
{
    private readonly Dictionary<long, CaptchaSession> _session = new();

    private readonly ILogger<CapthcaSessionService> _logger;

    public CapthcaSessionService(ILogger<CapthcaSessionService> logger)
    {
        _logger = logger;
    }

    public void CreateSession(long userId)
    {
        _session[userId] = new CaptchaSession
        {
            UserId = userId,
            Attempts = 0,
            ExpiryTime = DateTime.UtcNow.AddMinutes(3),
        };

        _logger.LogInformation($"Созданна сессия для пользователя {userId}");
    }

    public CaptchaSession? GetSession(long userId)
    {
        if (_session.TryGetValue(userId, out var session))
        {
            if (session.IsExpired)
            {
                _session.Remove(userId);
                return null;
            }

            return session;
        }
        return null;
    }

    public void RemoveCompletedSession(long userId)
    {
        _session.Remove(userId);
        _logger.LogInformation($"Сессия удалена для пользователя {userId}");
    }
}
