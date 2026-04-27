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
            ArticleId = x.ArticleId,
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
    
    public async Task CreateContainer(CreateContainerListRequest container, CancellationToken cancellationToken)
    {
        var cont = new Container
        {
            Id = container.Id,
            ArticleId = container.ArticleId,
            Address = container.Address,
            CategoryId = container.CategoryId,
            CreatedAt = DateTime.UtcNow,
            Condition = container.Condition,
            Currency = container.Currency,
            Latitude = container.Latitude,
            Longitude = container.Longitude,
            Quantity = container.Quantity,
            Username = container.Username,
            PriceType = container.PriceType,
            Price = container.Price,
            MessageId = container.MessageId,
            UserId = container.UserId,
        };
        dbContext.Containers.Add(cont);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<GetContainerListResponse>> GetContainerList(List<long>? ids, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        var result = dbContext.Containers
            .AsNoTracking()
            .AsQueryable();

        if (ids != null)
        {
            result = result.Where(x => ids.Contains(x.ArticleId));
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
                ArticleId = x.ArticleId,
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