var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TenementDB"))
);

builder.Services.Configure<TelegramBotSettings>(builder.Configuration.GetSection("TelegramBot"));

builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<TelegramBotSettings>>().Value;
    return new TelegramBotClient(settings.Token);
});

builder.Services.AddSingleton<CapthcaService>();
builder.Services.AddSingleton<SeleniumService>();
builder.Services.AddSingleton<NotificationService>();

builder.Services.AddHostedService<TelegramMessageHandler>();
builder.Services.AddHostedService<TelegramNotificator>();

builder.Services.AddLogging();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
dbContext.Database.Migrate();

app.MapGet("/", () => "Bot is running!");

app.Run();
