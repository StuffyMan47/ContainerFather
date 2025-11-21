using Microsoft.Extensions.Configuration;

namespace ContainerFather.Infrastructure.Services;

public class Settigns
{
    public string ApplicationName { get; init; }
    public AuthSettings AuthSettings { get; init; }
    public FileStorageSettings StorageSettings { get; init; }

    public FestifySettigns(IConfiguration configuration)
    {
        var applicationSection = configuration.GetSection("Application");

        ApplicationName = applicationSection["Name"] ?? "Unknown application name";
        AuthSettings = configuration.GetSection("Authentication").Get<AuthSettings>() ?? throw new InternalServerException("Не заданы настройки аутентификации");
    }
}