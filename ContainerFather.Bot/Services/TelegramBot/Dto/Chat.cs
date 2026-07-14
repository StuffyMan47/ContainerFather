namespace ContainerFather.Bot.Services.Dto;

public class ChatDialog
{
    public long Id { get; set; }
    public required string ChatName { get; set; }
    public List<long> MemberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}