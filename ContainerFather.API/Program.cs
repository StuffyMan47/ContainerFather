using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ContainerFather.Bot;
using ContainerFather.Bot.Services;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.Services.MaxBot;
using ContainerFather.Bot.Services.TelegramBot;
using ContainerFather.Configurations;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Infrastructure;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.Swagger;
using Hangfire;
using Max.Bot;
using Max.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddConfigurations();
    builder.Services
        .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
        .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

    await builder.Services.RegisterModule(builder.Configuration, builder.Environment);

    builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    var app = builder.Build();
    app.UseFestifyModule(app.Configuration, app.Environment);
    app.UseBotModule(app.Configuration);

    app.UseSwaggerBuilder(builder.Environment);

    var botClient = app.Services.GetRequiredService<TelegramBotClient>();
    var botService = app.Services.GetRequiredService<TelegramBotService>();
    
    var maxClient = app.Services.GetRequiredService<MaxClient>();
    var maxBotService = app.Services.GetRequiredService<MaxBotService>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    var maxHandler = new DelegatingUpdateHandler(
        onMessage: async (updateContext, ct) =>
        {
            try
            {
                // updateContext.Update содержит данные события от Max API
                await maxBotService.HandleUpdateAsync(updateContext.Update, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling Max bot update");
            }
        }
    );

// Запускаем polling в фоновой задаче
    using var maxCts = new CancellationTokenSource();
    _ = Task.Run(async () =>
    {
        try
        {
            logger.LogInformation("Starting Max bot in polling mode...");
            // Стартуем опрос сервера Max
            await maxClient.StartPollingAsync(maxHandler, cancellationToken: maxCts.Token);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Max bot polling crashed");
        }
    }, maxCts.Token);
    
    var commandService = app.Services.GetRequiredService<IStartCommandService>();
    await commandService.SetCommandsAsync();
    
    var receiverOptions = new ReceiverOptions
    {
        AllowedUpdates = Array.Empty<UpdateType>(), // получать все типы обновлений
    };
    
    using var cts = new CancellationTokenSource();
    
    _ = Task.Run(async () =>
    {
        try
        {
            logger.LogInformation("Starting bot in polling mode...");
        
            await botClient.ReceiveAsync(
                updateHandler: async (client, update, cancellationToken) =>
                {
                    try
                    {
                        await botService.HandleUpdateAsync(update, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error handling update");
                    }
                },
                errorHandler: async (client, exception, cancellationToken) =>
                {
                    logger.LogError(exception, "Polling error occurred");
                    await Task.CompletedTask;
                },
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Bot polling crashed");
            cts.Cancel();
        }
    });

// Эндпоинт для проверки статуса бота
    app.MapGet("/bot/status", () => new { status = "Bot is running in polling mode" });

    try
    {
        app.Run();
    }
    finally
    {
        cts.Cancel();
    }
// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using var scope = app.Services.CreateScope();
    await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    // var botService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
    // var baseUrl = builder.Configuration["BotConfiguration:WebhookUrl"];
    // var token = builder.Configuration["BotConfiguration:Token"];
    //
    // // Установка вебхука
    // var botClient = new TelegramBotClient(token);
    // var webhookUrl = $"{baseUrl}/api/bot";
    //
    // await botClient.SetWebhook(webhookUrl);

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    // StaticLogger.EnsureInitialized();
    // Log.Fatal(ex, "Приложение не может быть запущено из-за критической ошибки");
    Console.WriteLine("Приложение не может быть запущено из-за критической ошибки");
    throw;
}
finally
{
    // StaticLogger.EnsureInitialized();
    // Log.Information("Application shutdown...");
    // await Log.CloseAndFlushAsync();
    Console.WriteLine("Application shutdown...");
}

namespace ContainerFather
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Program;
}
