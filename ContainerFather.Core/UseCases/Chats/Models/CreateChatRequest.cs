namespace ContainerFather.Core.UseCases.Chats.Models;

public class CreateChatRequest
{
    public long Id { get; set; }
    public required string Name { get; set; }
}