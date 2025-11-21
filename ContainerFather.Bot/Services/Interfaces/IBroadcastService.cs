using ContainerFather.Bot.Services.Dto;

namespace ContainerFather.Bot.Services.Interfaces;

public interface IBroadcastService
{
    Task StartBroadcastSessionAsync(long userId);
    Task SelectChatAsync(long userId, long chatId, string chatName);
    Task ProcessBroadcastMessageAsync(long userId, string messageText);
    Task CancelBroadcastSessionAsync(long userId);
    BroadcastSession? GetSession(long userId);
    Task SendWeeklyBroadcastMessageAsync(long chatId, CancellationToken cancellationToken);
    Task SendDailyBroadcastMessageAsync(long chatId, CancellationToken cancellationToken);
}