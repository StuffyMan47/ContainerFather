using ContainerFather.Core.Enums;
using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.BroadcastMessages.Models;

namespace ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;

public interface IBroadcastMessageRepository : IScopedService
{
    Task<GetBroadcastMessageResponse?> GetBroadcastMessageById(long id);
    Task<List<GetBroadcastMessageListResponse>> GetBroadcastMessageList();

    Task<GetBroadcastMessageResponse?> GetActiveBroadcastMessage(BroadcastMessagePeriodType broadcastMessagePeriodType,
        CancellationToken cancellationToken);
    Task DeactivateAllAsync();
    Task<long> CreateBroadcastMessage(CreateBroadcastMessageRequest request, CancellationToken cancellationToken);
    Task UpdateBroadcastMessage(UpdateBroadcastMessageRequest request);
    Task DeleteBroadcastMessage();
}