using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.Containers.Models;

namespace ContainerFather.Core.UseCases.Containers.Interfaces;

public interface IContainerRepository : IScopedService
{
    Task CreateContainerList(List<CreateContainerListRequest> containers, CancellationToken cancellationToken);
    Task<List<GetContainerListResponse>> GetContainerList(List<long>? ids, DateTimeOffset? date, CancellationToken cancellationToken);
    Task CreateContainer(CreateContainerListRequest container, CancellationToken cancellationToken);
}