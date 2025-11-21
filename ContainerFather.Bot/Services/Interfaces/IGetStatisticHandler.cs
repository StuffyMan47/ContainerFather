namespace ContainerFather.Bot.Services.Interfaces;

public interface IGetStatisticHandler
{
    Task SendUserStatistic(long userId, long telegramChatId, CancellationToken cancellationToken);
    Task SendChatStatistic(long chatId, long telegramChatId, CancellationToken cancellationToken);
}