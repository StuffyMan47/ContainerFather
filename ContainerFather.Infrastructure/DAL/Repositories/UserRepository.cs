using ContainerFather.Core.Enums;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<long> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            CreatedAt = DateTime.UtcNow,
            TelegramId = request.TelegramId,
            Username = request.Username,
            LastActivity = DateTime.UtcNow,
            State = UserState.Active,
            Type = UserType.Average,
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return user.Id;
    }

    public async Task<GetUserStatisticResponse?> GetUserStatistic(long userId, CancellationToken cancellationToken)
    {
        var userInfo = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Username })
            .FirstOrDefaultAsync(cancellationToken);

        if (userInfo == null)
            return null;

        // Получаем статистику по чатам
        var chatStatistics = await dbContext.Messages
            .AsNoTracking()
            .Where(m => m.User.Id == userId) // Фильтруем по ID пользователя
            .Include(m => m.Chat)
            .GroupBy(m => new { m.ChatId, m.Chat.Name }) // Группируем по чату
            .Select(g => new ChatStatisticResponse
            {
                ChatName = g.Key.Name,
                MessageCount = g.Count(),
                Messages = g.OrderByDescending(m => m.CreatedAt)
                    .Take(10) // Берем последние 10 сообщений для примера
                    .Select(m => m.Content) // Используем Text вместо Content
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        // Собираем финальный результат
        var result = new GetUserStatisticResponse
        {
            Username = userInfo.Username ?? "Unknown",
            MessageCount = chatStatistics.Sum(c => c.MessageCount),
            Statistics = chatStatistics
        };

        return result;
    }

    public async Task<List<GetUserListResponse>> GetUserListByChatId(long chatId, CancellationToken cancellationToken)
    {
        var result = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Chats.Any(y=>y.Id == chatId) && u.State == UserState.Active)
            .Select(x=> new GetUserListResponse
            {
                TelegramId = x.TelegramId,
                Username = x.Username,
                Id = x.Id
            }).ToListAsync(cancellationToken);
        
        return result;
    }

    public async Task<List<GetUserListResponse>> GetUserList(GetUserListRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (request.OnlyActive)
        {
            query = query.Where(x => x.State == UserState.Active);
        }
        
        var result = await query
            .Select(x=> new GetUserListResponse
            {
                TelegramId = x.TelegramId,
                Username = x.Username,
                Id = x.Id
            }).ToListAsync(cancellationToken);
        
        return result;
    }

    public Task<GetUserByIdResponse?> GetUserById(long id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<GetUserByIdResponse?> GetByTelegramIdAsync(long id, CancellationToken cancellationToken)
    {
        var result = await dbContext.Users
            .Select(x=>new GetUserByIdResponse
            {
                Id = x.Id,
                Username = x.Username,
                TelegramId = x.TelegramId
            })
            .FirstOrDefaultAsync(x=>x.TelegramId == id, cancellationToken);
        return result;
    }

    public Task UpdateUser(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}