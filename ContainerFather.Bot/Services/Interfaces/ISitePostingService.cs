using ContainerFather.Bot.Services.Dto;

namespace ContainerFather.Bot.Services.Interfaces;

public interface ISitePostingService
{
    /// <summary>
    /// Отправка запроса на подтверждение админу
    /// </summary>
    /// <returns></returns>
    Task SendConfirmToAdmin();

    Task ReadGoogleTable(DateTimeOffset date);

    Task SendContainersToSite(List<ContainerRequestModel> containers);
}