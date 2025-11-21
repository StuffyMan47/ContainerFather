using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ContainerFather.Infrastructure.Swagger;

public static class Startup
{
    
    public const string ApplicationTitle = "ContainerFather";
    public const string AnonymousGroupName = "anonymous";

    public static IServiceCollection AddSwaggerBuilder(this IServiceCollection services)
    {
        
        services.AddSwaggerGen(setup =>
        {

            setup.SwaggerDoc(
                AnonymousGroupName,
                new()
                {
                    Title = ApplicationTitle,
                    Version = AnonymousGroupName
                }
            );

            // setup.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Api.xml"));
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerBuilder(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/{AnonymousGroupName}/swagger.json", AnonymousGroupName);
        });

        return app;
    }
}