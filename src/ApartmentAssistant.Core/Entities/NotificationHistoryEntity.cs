using ApartmentAssistant.Core.Entities;

public class NotificationHistoryEntity
{
    public Guid Id { get; set; }

    public long UserId { get; set; }

    public UserEntity? User { get; set; }

    public DateTimeOffset NotificationDateTime { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
