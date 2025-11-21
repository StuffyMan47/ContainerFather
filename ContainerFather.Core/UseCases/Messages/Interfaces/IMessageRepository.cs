using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.Messages.Models;

namespace ContainerFather.Core.UseCases.Messages.Interfaces;

public interface IMessageRepository : IScopedService
{
    Task<long> SaveMessageAsync(long telegramUserId, string text, long chatId);
    Task<GetMessageByIdResponse?> GetMessageById(long id);
    Task<List<GetMessageListResponse>> GetAllAsync();
    Task<List<GetMessageListResponse>> GetUserMessagesAsync(long userId);
}