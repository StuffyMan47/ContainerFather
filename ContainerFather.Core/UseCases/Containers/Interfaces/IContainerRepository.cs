using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.Containers.Models;

namespace ContainerFather.Core.UseCases.Containers.Interfaces;

public interface IContainerRepository : IScopedService
{
    Task CreateContainerList(List<CreateContainerListRequest> containers, CancellationToken cancellationToken);
    Task<List<GetContainerListResponse>> GetContainerList(List<Guid>? ids, DateTimeOffset? date, CancellationToken cancellationToken);
}