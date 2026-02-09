namespace ApartmentAssistant.Telegram.CaptchaSolver;

public class CaptchaSession
{
    public long UserId { get; set; }

    public int MaxAttempts { get; set; } = 3;

    public int Attempts { get; set; }

    public DateTime ExpiryTime { get; set; }

    public TenementIndicationEntity? Indications { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiryTime;
}
