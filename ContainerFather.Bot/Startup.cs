using ContainerFather.Bot.BackgroundJobs;
using ContainerFather.Bot.BackgroundJobs.Jobs;
using ContainerFather.Bot.Handlers;
using ContainerFather.Bot.Services;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Core.Interfaces.Settings;
using Hangfire;
using Hangfire.Storage;
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
        services.AddSingleton<TelegramBotClient>(provider =>
        {
            return new TelegramBotClient("8293970894:AAHDLNOTNuVhEi_jMk-aTYUAkKCY55s23ec");
        });
        services.AddScoped<TelegramBotService>();
        services.AddSingleton<IAdminDialogService, AdminDialogService>();
        services.AddScoped<IGetStatisticHandler, GetStatisticHandler>();
        services.AddScoped<IBroadcastService, BroadcastService>();
        services.AddScoped<IStartCommandService, StartCommandService>();
        services.AddScoped<SendDailyMessageJob>();
        services.AddScoped<SendWeeklyMessageJob>();
        
        services.AddBackgroundJobs(config);
        return services;
    }
    
    public static IApplicationBuilder UseBotModule(this IApplicationBuilder app, IConfiguration config)
    {
        app.UseHangfireDashboard(config);
        return app;
    }
}