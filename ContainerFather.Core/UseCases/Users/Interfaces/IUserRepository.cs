using ContainerFather.Core.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;

namespace ContainerFather.Core.UseCases.Users.Interfaces;

public interface IUserRepository : IScopedService
{
    Task<long> CreateUser(CreateUserRequest request, CancellationToken cancellationToken);
    Task<List<GetUserListResponse>> GetUserList(GetUserListRequest request, CancellationToken cancellationToken);
    Task<GetUserByIdResponse?> GetUserById(long id, CancellationToken cancellationToken);
    Task<GetUserByIdResponse?> GetByTelegramIdAsync(long id, CancellationToken cancellationToken);
    Task UpdateUser(UpdateUserRequest request, CancellationToken cancellationToken);
    Task<GetUserStatisticResponse?> GetUserStatistic(long userId, CancellationToken cancellationToken);
    Task<List<GetUserListResponse>> GetUserListByChatId(long chatId, CancellationToken cancellationToken);
}