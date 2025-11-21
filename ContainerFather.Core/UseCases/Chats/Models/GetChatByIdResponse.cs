namespace ContainerFather.Core.UseCases.Chats.Models;

public class GetChatByIdResponse
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public long TelegramId { get; set; }
}