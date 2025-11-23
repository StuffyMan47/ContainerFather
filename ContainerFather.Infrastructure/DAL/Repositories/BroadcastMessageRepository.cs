using ContainerFather.Core.Enums;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.BroadcastMessages.Models;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class BroadcastMessageRepository(AppDbContext dbContext) : IBroadcastMessageRepository
{
    public Task<GetBroadcastMessageResponse?> GetBroadcastMessageById(long id)
    {
        throw new NotImplementedException();
    }

    public Task<List<GetBroadcastMessageListResponse>> GetBroadcastMessageList()
    {
        throw new NotImplementedException();
    }

    public async Task<GetBroadcastMessageResponse?> GetActiveBroadcastMessage(BroadcastMessagePeriodType broadcastMessagePeriodType, CancellationToken cancellationToken)
    {
        var result = await dbContext.BroadcastMessages
            .AsNoTracking()
            .Where(x=>x.IsActive && x.PeriodType == broadcastMessagePeriodType)
            .Select(x=> new GetBroadcastMessageResponse
            {
                Message = x.Message
            }).FirstOrDefaultAsync(cancellationToken);
        
        return result;
    }

    public Task DeactivateAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<long> CreateBroadcastMessage(CreateBroadcastMessageRequest request, CancellationToken cancellationToken)
    {
        await dbContext.BroadcastMessages
            .Where(x=>x.PeriodType == request.PeriodType && x.IsActive)
            .ExecuteUpdateAsync(x=>x
                .SetProperty(y=>y.IsActive, false), cancellationToken);
        
        var newMessage = new BroadcastMessage
        {
            Message = request.Message,
            PeriodType = request.PeriodType,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.BroadcastMessages.Add(newMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return newMessage.Id;
    }

    public Task UpdateBroadcastMessage(UpdateBroadcastMessageRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task DeactivateBroadcastMessage(BroadcastMessagePeriodType periodType, CancellationToken cancellationToken)
    {
        await dbContext.BroadcastMessages
            .Where(x=>x.PeriodType == periodType)
            .ExecuteUpdateAsync(x=>x
                .SetProperty(y=> y.IsActive, false), 
                cancellationToken);
    }
}