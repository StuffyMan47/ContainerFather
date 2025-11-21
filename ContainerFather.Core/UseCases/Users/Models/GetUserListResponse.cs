namespace ContainerFather.Core.UseCases.Users.Models;

public class GetUserListResponse
{
    public long Id { get; set; }
    public long TelegramId { get; set; }
    public required string Username { get; set; }
}