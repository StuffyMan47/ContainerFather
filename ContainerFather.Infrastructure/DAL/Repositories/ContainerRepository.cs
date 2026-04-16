using ContainerFather.Core.UseCases.Containers.Interfaces;
using ContainerFather.Core.UseCases.Containers.Models;
using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class ContainerRepository(AppDbContext dbContext) : IContainerRepository
{
    public async Task CreateContainerList(List<CreateContainerListRequest> containers, CancellationToken cancellationToken)
    {
        dbContext.Containers.AddRange(containers.Select(x=>new Container
        {
            Id = x.Id,
            Address = x.Address,
            CategoryId = x.CategoryId,
            CreatedAt = DateTime.UtcNow,
            Condition = x.Condition,
            Currency = x.Currency,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Quantity = x.Quantity,
            Username = x.Username,
            PriceType = x.PriceType,
            Price = x.Price,
            MessageId = x.MessageId,
            UserId = x.UserId,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<GetContainerListResponse>> GetContainerList(List<Guid>? ids, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        var result = dbContext.Containers
            .AsNoTracking()
            .AsQueryable();

        if (ids != null)
        {
            result = result.Where(x => ids.Contains(x.Id));
        }

        if (date.HasValue)
        {
            var startOfDay = date.Value.Date;
            var nextDay = startOfDay.AddDays(1);
        
            result = result.Where(x => x.CreatedAt >= startOfDay && x.CreatedAt < nextDay);
        }
        
        return await result
            .Select(x=> new GetContainerListResponse
            {
                Id = x.Id,
                Address = x.Address,
                CategoryId = x.CategoryId,
                CreatedAt = DateTime.UtcNow,
                Condition = x.Condition,
                Currency = x.Currency,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Quantity = x.Quantity,
                Username = x.Username,
                PriceType = x.PriceType,
                Price = x.Price,
            })
            .ToListAsync(cancellationToken);
    }
}