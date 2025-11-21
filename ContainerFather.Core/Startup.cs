using System.Linq.Expressions;
using ContainerFather.Core.UseCases.BroadcastMessages;
using ContainerFather.Core.UseCases.Messages;
using ContainerFather.Core.UseCases.Users;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerFather.Core;

public static class Startup
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<UserUseCase>();
        services.AddScoped<MessageUseCase>();
        services.AddScoped<BroadcastMessageUseCase>();

        return services;
    }
}