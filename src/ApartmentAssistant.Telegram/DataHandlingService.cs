public class DataHandlingService
{
    private readonly ApplicationDbContext _db;

    private readonly ILogger<DataHandlingService> _logger;

    public DataHandlingService(ApplicationDbContext context, ILogger<DataHandlingService> logger)
    {
        _db = context;
        _logger = logger;
    }

    public async Task SaveUserIndicationAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: string text } message)
            return;

        var userId = message.Chat.Id;

        var userIndications = text.Split(",")
            .Select(u => decimal.TryParse(u.Trim(), out decimal result) ? result : 0m)
            .ToList();

        if (userIndications.Count < 4)
        {
            throw new ArgumentException(
                "Недостаточно показаний. Ожидается 4 значения через запятую."
            );
        }
        else if (userIndications.Any(u => u.Equals(0)))
            throw new Exception();

        var userDataToSave = new TenementIndicationEntity()
        {
            Id = Guid.NewGuid(),
            UserId = (int)userId,
            BathroomHotWater = userIndications[0],
            BathroomColdWater = userIndications[1],
            KitchenHotWater = userIndications[2],
            KitchenColdWater = userIndications[3],
            ContributionDate = DateTimeOffset.UtcNow.AddHours(5),
        };

        await _db.Set<TenementIndicationEntity>().AddAsync(userDataToSave, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
