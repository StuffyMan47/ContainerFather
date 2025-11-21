using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class ChatRepository(AppDbContext dbContext) : IChatRepository
{
    public async Task<long> CreateChat(CreateChatRequest request, CancellationToken cancellationToken)
    {
        var chat = new Chat()
        {
            TelegramId = request.Id,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            Description = null
        };
        dbContext.Chats.Add(chat);
        await dbContext.SaveChangesAsync(cancellationToken);
        return chat.Id;
    }

    public async Task<List<GetChatListResponse>> GetChatLists(CancellationToken cancellationToken)
    {
        var result = await dbContext.Chats.Select(x => new GetChatListResponse()
        {
            ChatId = x.Id,
            ChatName = x.Name,
            TelegramId = x.TelegramId,
        }).ToListAsync(cancellationToken);
        
        return result;
    }

    public async Task<GetChatStatisticResponse?> GetChatStatistic(long chatId, CancellationToken cancellationToken)
    {
        var result = await dbContext.Chats
            .AsNoTracking()
            .Where(c => c.Id == chatId)
            .Include(x => x.Messages)
            .ThenInclude(x => x.User)
            .Select(x => new GetChatStatisticResponse
            {
                ChatName = x.Name,
                MessageCount = x.Messages.Count,
                PersonalStatistics = x.Messages
                    .GroupBy(y => y.UserId)
                    .Select(y => new PersonalStatistic
                    {
                        UserId = y.Key,
                        UserName = y.First().User.Username,
                        MessageCount = y.Count()
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        return result;
    }

    public async Task<List<GetChatMembers>> GetChatMembers(long chatId, CancellationToken cancellationToken)
    {
        var result = await dbContext.Chats
            .AsNoTracking()
            .Where(x=>x.Id == chatId)
            .SelectMany(x=>x.Users)
            .Select(x=> new GetChatMembers
            {
                Username = x.Username,
                UserTelegramId = x.TelegramId,
                ChatId = chatId,
            }).ToListAsync(cancellationToken);
        
        return result;
    }

    public async Task<GetChatByIdResponse?> GetChatById(long chatId, CancellationToken cancellationToken)
    {
        var result = await dbContext.Chats
            .Where(x => x.Id == chatId)
            .Select(x=> new GetChatByIdResponse
            {
                Id = x.Id,
                Name = x.Name,
                TelegramId = x.TelegramId
            }).FirstOrDefaultAsync(cancellationToken);
        return result;
    }
}