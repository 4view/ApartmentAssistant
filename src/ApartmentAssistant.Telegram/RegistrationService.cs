public class RegistrationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<RegistrationService> _logger;

    private readonly ITelegramBotClient _botClient;

    public RegistrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistrationService> logger,
        ITelegramBotClient telegramBotClient
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _botClient = telegramBotClient;
    }

    public async Task<UserEntity?> FindUserAsync(long chatId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await db.Set<UserEntity>().FirstOrDefaultAsync(u => u.Id == chatId);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Пользователя {chatId} не удалось найти!", ex.Message);
            return null;
        }
    }

    public async Task ProcessRegistration(long chatId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(chatId);

        if (user is not null)
        {
            await _botClient.SendMessage(
                chatId,
                $"Пользователь уже зарегестрирован",
                cancellationToken: cancellationToken
            );
        }

        await _botClient.SendMessage(
            chatId,
            $"Введите логин и пароль в формате: \nвашЛогин\nвашПароль",
            cancellationToken: cancellationToken
        );

        
    }
}
