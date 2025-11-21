namespace ContainerFather.Bot.States;

public enum AdminDialogState
{
    None,
    WaitingForChatId,
    WaitingForUserId,
    ManagingWeeklyMessage,
    WaitingForNewWeeklyMessage,
    ManagingDailyMessage,
    WaitingForNewDailyMessage
}