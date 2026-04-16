using ContainerFather.Infrastructure.DAL.DbContext;
using ContainerFather.Infrastructure.DAL.Entites;

namespace ContainerFather.Infrastructure.DAL.Repositories;

public class LlmErrorRepository(AppDbContext dbContext)
{
    public async Task<long> CreateErrorMessage(CancellationToken cancellationToken)
    {
        var newError = new LlmError
        {
            TelegramMessage = string.Empty,
            Prompt = string.Empty,
            ErrorMessage = string.Empty,
            ContainerResponse = null,
            LlmRequest = null,
            LlmResponse = null,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.LlmErrors.Add(newError);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return newError.Id;
    }
}