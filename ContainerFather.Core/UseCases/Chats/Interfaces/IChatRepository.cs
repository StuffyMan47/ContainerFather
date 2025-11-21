using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;

namespace ContainerFather.Core.UseCases.Chats.Interfaces;

public interface IChatRepository : IScopedService
{
    Task<GetChatStatisticResponse?> GetChatStatistic(long chatId, CancellationToken cancellationToken);
    Task<long> CreateChat(CreateChatRequest request, CancellationToken cancellationToken);
    Task<List<GetChatListResponse>> GetChatLists(CancellationToken cancellationToken);
    Task<List<GetChatMembers>> GetChatMembers(long chatId, CancellationToken cancellationToken);
    Task<GetChatByIdResponse?> GetChatById(long chatId, CancellationToken cancellationToken);
}