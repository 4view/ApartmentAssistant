public class NotificationService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationService> _logger,
    ITelegramBotClient _botClient
)
{
    private readonly DateTimeOffset LocalTime = DateTimeOffset.Now;

    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "\u001b[32mPROCESS: Началась выборка пользователей, которым нужно отправить уведомление"
        );
        var users = await UserToNotificateAsync();

        var now = DateTimeOffset.Now;
        var currentTime = now.TimeOfDay;

        var notificationTimes = new[]
        {
            new TimeOnly(10, 0, 0),
            new TimeOnly(13, 4, 0),
            new TimeOnly(13, 11, 0),
        };

        foreach (var time in notificationTimes)
        {
            if (Math.Abs((currentTime - time.ToTimeSpan()).TotalMinutes) <= 1)
            {
                _logger.LogInformation("\u001b[32mPROCESS: Отправка уведомления пользователям!");
                foreach (var user in users)
                {
                    if (!await WasNotifiedToday(user, time))
                    {
                        await _botClient.SendMessage(
                            user.Id,
                            "Test notification",
                            cancellationToken: cancellationToken
                        );

                        await AddUserNotificationHistoryAsync(user, time);
                    }
                    else
                    {
                        _logger.LogInformation($"Пользователь {user} уже был уведомлен в {time}");
                        break;
                    }
                }
            }
            else
            {
                _logger.LogInformation($"Время {time} еще не наступило");
            }
        }
    }

    public async Task<bool> WasNotifiedToday(UserEntity user, TimeOnly time)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var notificationTime = GetDateWithSpecifiedTime(time);

            var wasNotified = await db.Set<NotificationHistoryEntity>()
                .AnyAsync(t => t.UserId == user.Id && t.NotificationDateTime == notificationTime);

            return wasNotified;
        }
    }

    /// <summary>
    /// Проверяем начался ли период, когда необходимо начать уведомлять пользователей
    /// </summary>
    public bool IsNotificationPeriodStart()
    {
        if (LocalTime.Day >= 2 && LocalTime.Day <= 25)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Добовляем запись в историю уведомлений
    /// </summary>
    /// <param name="user">пользователь которому пришло уведомление</param>
    public async Task AddUserNotificationHistoryAsync(UserEntity user, TimeOnly notificationTime)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var notificationDateTime = GetDateWithSpecifiedTime(notificationTime);

            var userNotification = new NotificationHistoryEntity
            {
                UserId = user.Id,
                NotificationDateTime = notificationDateTime,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Set<NotificationHistoryEntity>().Add(userNotification);
            await db.SaveChangesAsync();
        }
    }

    private DateTimeOffset GetDateWithSpecifiedTime(TimeOnly time)
    {
        var currentDateTime = DateTimeOffset.UtcNow;

        var dateOnly = DateOnly.FromDateTime(currentDateTime.Date);
        var dateTime = dateOnly.ToDateTime(time);

        return new DateTimeOffset(dateTime, currentDateTime.Offset);
    }

    /// <summary>
    /// Получаем список пользователй которых необходимо уведомить
    /// </summary>
    public async Task<List<UserEntity>> UserToNotificateAsync()
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currentMonth = DateTimeOffset.Now.Month;

            var userToNotify = db.Set<UserEntity>()
                .Where(u =>
                    !db.Set<TenementIndicationEntity>()
                        .Where(t => t.UserId == u.Id)
                        .Any(t => t.ContributionDate.Month == currentMonth)
                )
                .ToListAsync();

            return await userToNotify;
        }
    }
}
