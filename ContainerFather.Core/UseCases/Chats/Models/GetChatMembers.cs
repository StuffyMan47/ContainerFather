namespace ContainerFather.Core.UseCases.Chats.Models;

public class GetChatMembers
{
    public long ChatId { get; set; }
    public long UserTelegramId { get; set; }
    public required string Username { get; set; }
    
}