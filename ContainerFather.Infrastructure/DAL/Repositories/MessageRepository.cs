using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Messages.Models;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class MessageRepository(AppDbContext dbContext) : IMessageRepository
{
    public async Task<long> SaveMessageAsync(long telegramUserId, string text, long chatId)
    {
        var chat = await dbContext.Chats
            .AsNoTracking()
            .FirstAsync(x=>x.TelegramId == chatId);
        
        var message = new Message
        {
            Content = text,
            UserId = telegramUserId,
            ChatId = chat.Id
        };
        
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync();
        
        return message.Id;
    }

    public Task<GetMessageByIdResponse?> GetMessageById(long id)
    {
        throw new NotImplementedException();
    }

    public Task<List<GetMessageListResponse>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<GetMessageListResponse>> GetUserMessagesAsync(long userId)
    {
        throw new NotImplementedException();
    }
}