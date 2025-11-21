namespace ContainerFather.Bot.States;

public enum AdminState
{
    None,
    WaitingForChatId,
    WaitingForUserId,
    ManagingWeeklyMessage,
    WaitingForNewWeeklyMessage
}