using ContainerFather.Bot.SiteService.Model;

namespace ContainerFather.Bot.SiteService;

public interface ISiteClient
{
    Task SendContainersInfo(List<SendContainersInfoRequest> request, CancellationToken cancellationToken);
}