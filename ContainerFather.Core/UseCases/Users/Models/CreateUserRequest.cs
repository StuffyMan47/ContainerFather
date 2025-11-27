using ContainerFather.Core.Enums;

namespace ContainerFather.Core.UseCases.Users.Models;

public class CreateUserRequest
{
    public required long TelegramId { get; set; }
    public required string Username { get; set; }
    public UserType UserType { get; set; } = UserType.Subscriber;
}