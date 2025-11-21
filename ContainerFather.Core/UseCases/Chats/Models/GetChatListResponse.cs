namespace ContainerFather.Core.UseCases.Chats.Models;

public class GetChatListResponse
{
    public long ChatId { get; set; }
    public long TelegramId { get; set; }
    public required string ChatName { get; set; }
}