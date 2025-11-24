using ContainerFather.Bot.BackgroundJobs.Jobs;
using Hangfire;
using Hangfire.Console;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerFather.Bot.BackgroundJobs;

internal static class Startup
{
    internal static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration config)
    {
        string? connectionString = config.GetConnectionString("DBConnectionString");
        if (string.IsNullOrEmpty(connectionString)) throw new Exception("Hangfire Storage Provider: Не указана строка подключения");

        services
            .AddHangfire((hangfireConfig) => hangfireConfig
                .UseConsole()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options => config.GetSection("HangfireSettings:Server").Bind(options));
        
        services.AddScoped<SendDailyMessageJob>();
        services.AddScoped<SendWeeklyMessageJob>();
        return services;
    }

    internal static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var dashboardOptions = config.GetSection("HangfireSettings:Dashboard").Get<DashboardOptions>() ?? throw new Exception("Hangfire Dashboard: отсутствуют опции в конфиге");
 
        app.UseHangfireDashboard(config["HangfireSettings:Route"], dashboardOptions);

        IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();
        var serversToRemove = monitoringApi.Servers();
        foreach (var server in serversToRemove)
        {
            JobStorage.Current.GetConnection().RemoveServer(server.Name);
        }

        app.ApplicationServices.AddRecurringJobs();
        return app;
    }

    public static void AddRecurringJobs(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.RemoveIfExists("daily-message");
        recurringJobManager.AddOrUpdate<SendDailyMessageJob>(
            "daily-message",
            service => service.Execute(),
            Cron.Daily(hour: 6)
        );
        
        recurringJobManager.RemoveIfExists("weekly-message");
        recurringJobManager.AddOrUpdate<SendWeeklyMessageJob>(
            "weekly-message",
            service => service.Execute(),
            Cron.Weekly(DayOfWeek.Monday, 10)
        );
    }
}