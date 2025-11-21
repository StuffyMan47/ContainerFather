namespace ContainerFather.Core.UseCases.Users.Models;

public class GetUserByIdResponse
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public  long TelegramId { get; set; }
    public List<long> ChatIds { get; set; } = [];
}