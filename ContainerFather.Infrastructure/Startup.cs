using System.Net.Mime;
using System.Reflection;
using ContainerFather.Bot;
using ContainerFather.Core;
using ContainerFather.Core.Interfaces.Settings;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Infrastructure.DAL;
using ContainerFather.Infrastructure.Services;
using ContainerFather.Infrastructure.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerFather.Infrastructure;

public static class Startup
{
    public static async Task<IServiceCollection> RegisterModule(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        Assembly[] assemblies =
        [
            typeof(Bot.Startup).Assembly,
            typeof(Core.Startup).Assembly,
            typeof(Startup).Assembly,
        ];
        services.AddHttpContextAccessor();
        services.AddDataAccessLayer(config, environment);
        services.AddApplicationLayer();
        services.AddBotLayer(config);
        services.AddSingleton<ISetting, Setting>();

        // services.AddValidationBuilder();

        services.RegisterServicesByInterfaces(assemblies);
        services.AddSwaggerBuilder();
        return await Task.FromResult(services);
    }
    
    public static IApplicationBuilder UseFestifyModule(this IApplicationBuilder app, IConfiguration config, IWebHostEnvironment environment)
    {
        app.UseSwaggerBuilder(environment);
        
        return app;
    }
}
