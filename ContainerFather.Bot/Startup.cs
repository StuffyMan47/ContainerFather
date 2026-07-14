using ContainerFather.Bot.BackgroundJobs;
using ContainerFather.Bot.BackgroundJobs.Jobs;
using ContainerFather.Bot.Handlers;
using ContainerFather.Bot.Services;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.Services.MaxBot;
using ContainerFather.Bot.Services.TelegramBot;
using ContainerFather.Core.Interfaces.Settings;
using Hangfire;
using Hangfire.Storage;
using Max.Bot;
using Max.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Telegram.Bot;


namespace ContainerFather.Bot;

public static class Startup
{
    public static IServiceCollection AddBotLayer(this IServiceCollection services, IConfiguration config)
    {
        var telegaBotToken = config.GetSection("BotConfiguration").GetSection("TelegramToken");
        services.AddSingleton<TelegramBotClient>(provider =>
        {
            var options = new TelegramBotClientOptions(telegaBotToken.Value);
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(360) };
            return new TelegramBotClient(options, httpClient);
        });
        
        var maxBotToken = config.GetSection("BotConfiguration").GetSection("MaxToken");
        services.AddSingleton<MaxClient>(provider =>
        {
            return new MaxClient(new MaxBotOptions
            {
                Token = maxBotToken.Value,
            });
        });
        
        services.AddScoped<TelegramBotService>();
        services.AddScoped<MaxBotService>();
        services.AddSingleton<IAdminDialogService, AdminDialogService>();
        services.AddScoped<IGetStatisticHandler, GetStatisticHandler>();
        services.AddScoped<IBroadcastService, BroadcastService>();
        services.AddScoped<IStartCommandService, StartCommandService>();
        services.AddScoped<SendDailyMessageJob>();
        services.AddScoped<SendWeeklyMessageJob>();
        services.AddScoped<SendConfirmToSitePostJob>();
        services.AddScoped<ISitePostingService, SitePostingService>();
        
        services.AddBackgroundJobs(config);
        return services;
    }
    
    public static IApplicationBuilder UseBotModule(this IApplicationBuilder app, IConfiguration config)
    {
        app.UseHangfireDashboard(config);
        return app;
    }
}