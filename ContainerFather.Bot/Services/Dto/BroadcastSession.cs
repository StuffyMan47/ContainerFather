using ContainerFather.Bot.States;

namespace ContainerFather.Bot.Services.Dto;

public class BroadcastSession
{
    public long UserId { get; set; }
    public long? SelectedChatId { get; set; }
    public string? SelectedChatName { get; set; }
    public BroadcastState State { get; set; } = BroadcastState.Idle;
}