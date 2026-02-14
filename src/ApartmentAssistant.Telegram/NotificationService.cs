public class NotificationService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationService> _logger,
    ITelegramBotClient _botClient
)
{
    private readonly DateTimeOffset LocalTime = DateTimeOffset.Now;

    /// <summary>
    /// Проверяет и уведомляет пользователя в случае отсутсвия уведомлений
    /// </summary>
    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "\u001b[32mPROCESS: Началась выборка пользователей, которым нужно отправить уведомление"
        );
        var users = await UserToNotificateAsync(cancellationToken);

        var now = DateTimeOffset.Now;
        var currentTime = now.TimeOfDay;

        var notificationTimes = new[]
        {
            new TimeOnly(12, 40, 00),
            new TimeOnly(16, 14, 0),
            new TimeOnly(22, 16, 50),
        };

        foreach (var time in notificationTimes)
        {
            var timeSpan = time.ToTimeSpan();
            var timeDifference = timeSpan - currentTime;

            if (Math.Abs((currentTime - time.ToTimeSpan()).TotalSeconds) <= 10)
            {
                _logger.LogInformation("\u001b[32mPROCESS: Отправка уведомления пользователям!");
                foreach (var user in users)
                {
                    if (!await WasNotifiedToday(user, time))
                    {
                        await NotificateUser(user.Id, cancellationToken);

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
                if (timeDifference > TimeSpan.Zero)
                {
                    _logger.LogInformation(
                        $"Время {time} еще не наступило, до наступления {timeDifference:hh\\:mm\\:ss}"
                    );
                }
                else
                {
                    _logger.LogInformation(
                        $"Время {time} уже прошло {(-timeDifference):hh\\:mm\\:ss} назад"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Сообщение уведомляющее пользователя о небходимости внести показания
    /// </summary>
    /// <param name="chatId">Чат пользователя</param>
    private async Task NotificateUser(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(
                    "Внести показания",
                    "\nКухня:\n"
                        + "Горячая вода =\n"
                        + "Холодная вода =\n\n"
                        + "Ванная:\n"
                        + "Горячая вода =\n"
                        + "Холодная вода ="
                ),
            }
        );

        await _botClient.SendMessage(
            chatId,
            "НАПОМИНАНИЕ! \nНеобходимо внести показаний счетчиков: ",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Проверяет, был ли пользователь <paramref name="user"/> уведомлен в это <paramref name="time"/> время
    /// </summary>
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
    /// Проверяет начался ли период, когда необходимо начать уведомлять пользователей
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
    /// Добовляет запись уведомления пользователя в бд
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

    /// <summary>
    /// Формирует дату с определенным <paramref name="time"/> временем
    /// </summary>
    private DateTimeOffset GetDateWithSpecifiedTime(TimeOnly time)
    {
        var currentDateTime = DateTimeOffset.UtcNow;

        var dateOnly = DateOnly.FromDateTime(currentDateTime.Date);
        var dateTime = dateOnly.ToDateTime(time);

        return new DateTimeOffset(dateTime, currentDateTime.Offset);
    }

    /// <summary>
    /// Получает список пользователй которых необходимо уведомить
    /// </summary>
    public async Task<List<UserEntity>> UserToNotificateAsync(CancellationToken cancellationToken)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currentMonth = DateTimeOffset.Now.Month;

            var userToNotify = await db.Set<UserEntity>()
                .Where(u =>
                    !db.Set<TenementIndicationEntity>()
                        .Where(t => t.UserId == u.Id)
                        .Any(t => t.ContributionDate.Month == currentMonth)
                )
                .ToListAsync(cancellationToken);

            return userToNotify;
        }
    }
}
