using System.Text;
using ContainerFather.Core.Interfaces.Settings;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.BroadcastMessages.Models;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ContainerFather.Bot.Services;

public class TelegramBotService(
    IUserRepository userRepository,
    IMessageRepository messageRepository,
    IBroadcastMessageRepository broadcastMessageRepository,
    IChatRepository chatRepository,
    IOptions<ISetting> options
    ) 
{
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;
        var telegramUserId = message.From?.Id;
        var text = message.Text ?? string.Empty;

        if (telegramUserId == null) return;

        // Сохраняем пользователя
        var userId = await SaveOrUpdateUserAsync(message.From!, cancellationToken);

        // Определяем тип чата
        bool isGroupChat = message.Chat.Type is ChatType.Group or ChatType.Supergroup;

        // Сохраняем сообщение
        await messageRepository.SaveMessageAsync(userId, text, chatId);

        // Обработка команд
        if (text.StartsWith('/'))
        {
            await HandleCommandAsync(message, text, cancellationToken);
        }
    }
    
    private async Task<long> SaveOrUpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByTelegramIdAsync(user.Id, cancellationToken);
        long userId;
        if (existingUser == null)
        {
            var newUser = new CreateUserRequest
            {
                TelegramId = user.Id,
                Username = user.Username,
            };
            userId = await userRepository.CreateUser(newUser, cancellationToken);
            return userId;
        }
        
        userId = existingUser.Id;
        return userId;
    }
    
    private async Task HandleCommandAsync(global::Telegram.Bot.Types.Message message, string command, CancellationToken cancellationToken)
    {
        var adminId = long.Parse(options.Value.BotConfiguration.Token);
        
        if (message.From?.Id != adminId) return;

        switch (command.ToLower())
        {
            case "/start":
                await SaveOrUpdateUserAsync(message.From!, cancellationToken);
                break;
            case "/broadcast":
                await SendBroadcastInstructionsAsync(message.Chat.Id);
                break;
            case "/setweeklymessage":
                await SetWeeklyMessageInstructionsAsync(message.Chat.Id);
                break;
            case "/getStatistic":
                await SendStatisticsByChatId(message.Chat.Id);
                break;
            default:
                if (command.StartsWith("/broadcast "))
                {
                    await HandleBroadcastAsync(message, command["/broadcast ".Length..], cancellationToken);
                }
                else if (command.StartsWith("/setweeklymessage "))
                {
                    await HandleSetWeeklyMessageAsync(message, command["/setweeklymessage ".Length..]);
                }
                break;
        }
    }
    
    private async Task SendBroadcastInstructionsAsync(long chatId)
    {
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);
        await botClient.SendMessage(
            chatId,
            "Для рассылки сообщения всем пользователям используйте команду:\n" +
            "/broadcast ваш_текст_сообщения");
    }

    private async Task SendStatisticsByChatId(long chatId)
    {
        var result = await chatRepository.GetChatStatistic(chatId, CancellationToken.None);
        
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);
        await botClient.SendMessage(
            chatId,
            "");
    }

    private string GetStatisticMessage(GetChatStatisticResponse statistic)
    {
        if (statistic.MessageCount == 0)
            return "В чате нет сообщений для статистики.";

        var orderedStats = statistic.PersonalStatistics
            .OrderByDescending(x => x.MessageCount)
            .ToList();

        var message = $"СТАТИСТИКА ЧАТА: {statistic.ChatName}\n\n";
    
        // Заголовок таблицы
        message += $"№  | Участник | Сообщений | Доля\n";
        message += $"---|----------|-----------|------\n";

        for (int i = 0; i < orderedStats.Count; i++)
        {
            var user = orderedStats[i];
            var percentage = (double)user.MessageCount / statistic.MessageCount * 100;
        
            // Обрезаем длинные имена пользователей для таблицы
            var userName = user.UserName.Length > 15 
                ? user.UserName.Substring(0, 12) + "..." 
                : user.UserName;

            message += $"{i + 1,2} | {userName,-15} | {user.MessageCount,9} | {percentage:0.0}%\n";
        }

        message += $"\nИтого: {statistic.MessageCount} сообщений от {statistic.PersonalStatistics.Count} участников";

        return message;
    }

    private async Task SetWeeklyMessageInstructionsAsync(long chatId)
    {
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);
        await botClient.SendMessage(
            chatId,
            "Для установки еженедельного сообщения используйте команду:\n" +
            "/setweeklymessage ваш_текст_сообщения");
    }
    
    private async Task HandleBroadcastAsync(global::Telegram.Bot.Types.Message message, string broadcastText, CancellationToken cancellationToken)
    {
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);

        if (string.IsNullOrWhiteSpace(broadcastText))
        {
            await botClient.SendMessage(message.Chat.Id, "Текст рассылки не может быть пустым.");
            return;
        }

        var users = await userRepository.GetUserList(new GetUserListRequest
        {
            OnlyActive = true
        }, cancellationToken);
        var successCount = 0;
        
        foreach (var user in users)
        {
            try
            {
                await botClient.SendMessage(user.TelegramId, broadcastText);
                successCount++;
                await Task.Delay(50); // Задержка чтобы не превысить лимиты Telegram
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но продолжаем рассылку
                Console.WriteLine($"Ошибка отправки пользователю {user.TelegramId}: {ex.Message}");
            }
        }
        await botClient.SendMessage(
            message.Chat.Id,
            $"Рассылка завершена. Успешно отправлено: {successCount} из {users.Count} пользователей.");
    }
    
    private async Task HandleSetWeeklyMessageAsync(global::Telegram.Bot.Types.Message message, string weeklyMessage)
    {
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);

        if (string.IsNullOrWhiteSpace(weeklyMessage))
        {
            await botClient.SendMessage(message.Chat.Id, "Текст еженедельного сообщения не может быть пустым.");
            return;
        }

        var broadcastMessage = new CreateBroadcastMessageRequest()
        {
            // Message = weeklyMessage,
            // IsActive = true,
            // CreatedAt = DateTime.UtcNow
        };

        await broadcastMessageRepository.CreateBroadcastMessage(broadcastMessage);

        await botClient.SendMessage(
            message.Chat.Id,
            "Еженедельное сообщение установлено!");
    }

    public async Task SendWeeklyBroadcastAsync(CancellationToken cancellationToken)
    {
        var botClient = new TelegramBotClient(options.Value.BotConfiguration.Token);

        var weeklyMessage = await broadcastMessageRepository.GetActiveBroadcastMessage();
        if (weeklyMessage == null) return;

        var users = await userRepository.GetUserList(new GetUserListRequest
        {
            OnlyActive = true
        }, cancellationToken);

        foreach (var user in users)
        {
            try
            {
                await botClient.SendMessage(user.TelegramId, weeklyMessage.Message);
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка еженедельной рассылки пользователю {user.TelegramId}: {ex.Message}");
            }
        }
    }
}