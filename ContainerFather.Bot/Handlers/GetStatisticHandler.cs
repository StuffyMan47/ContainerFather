using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace ContainerFather.Bot.Handlers;

public class GetStatisticHandler(
    IUserRepository userRepository,
    IMessageRepository messageRepository,
    IBroadcastMessageRepository broadcastMessageRepository,
    IChatRepository chatRepository,
    IAdminDialogService adminDialogService,
    IOptions<BotConfiguration> options
    ) : IGetStatisticHandler
{
    public async Task SendUserStatistic(long userId, long telegramChatId, CancellationToken cancellationToken)
    {
        var botClient = new TelegramBotClient(options.Value.Token);

        var userStatistic = await userRepository.GetUserStatistic(userId, cancellationToken);

        var response = FormatUserStatisticForAdmin(userStatistic);
        await botClient.SendMessage(
            telegramChatId,
            response,
            cancellationToken: cancellationToken);
    }
    
    public async Task SendChatStatistic(long chatId, long telegramChatId, CancellationToken cancellationToken)
    {
        var botClient = new TelegramBotClient(options.Value.Token);

        // Получаем статистику по чату
        var statistic = await chatRepository.GetChatStatistic(chatId, CancellationToken.None);

        if (statistic.MessageCount == 0)
        {
            await botClient.SendMessage(
                telegramChatId,
                $"Чат с ID {chatId} не содержит сообщений",
                cancellationToken: cancellationToken);
        }
        else
        {
            var response = FormatStatisticForAdmin(statistic);
            await botClient.SendMessage(
                telegramChatId,
                response,
                cancellationToken: cancellationToken);
        }
    }
    
    private string FormatStatisticForAdmin(GetChatStatisticResponse statistic)
    {
        var message = $"СТАТИСТИКА ЧАТА\n";
        message += $"Чат: {statistic.ChatName}\n";
        message += $"Сообщений всего: {statistic.MessageCount}\n";
        message += $"Участников: {statistic.PersonalStatistics.Count}\n\n";

        message += "СТАТИСТИКА ПО УЧАСТНИКАМ:\n";

        foreach (var userStat in statistic.PersonalStatistics.OrderByDescending(x => x.MessageCount))
        {
            var percentage = (double)userStat.MessageCount / statistic.MessageCount * 100;
            message += $"- {userStat.UserName}: {userStat.MessageCount} сообщ. ({percentage:0.0}%)\n";
        }

        return message;
    }
    
    private string FormatUserStatisticForAdmin(GetUserStatisticResponse userStatistic)
    {
        var message = $"СТАТИСТИКА ПОЛЬЗОВАТЕЛЯ\n";
        message += $"Пользователь: {userStatistic.Username}\n";
        message += $"Всего сообщений: {userStatistic.MessageCount}\n\n";
    
        message += "РАСПРЕДЕЛЕНИЕ ПО ЧАТАМ:\n";
    
        foreach (var chatStat in userStatistic.Statistics.OrderByDescending(x => x.MessageCount))
        {
            var percentage = (double)chatStat.MessageCount / userStatistic.MessageCount * 100;
            message += $"- {chatStat.ChatName}: {chatStat.MessageCount} сообщ. ({percentage:0.0}%)\n";
        }

        return message;
    }
}