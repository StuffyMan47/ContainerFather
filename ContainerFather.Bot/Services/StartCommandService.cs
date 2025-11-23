using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Core.Interfaces.Settings.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace ContainerFather.Bot.Services;

public class StartCommandService : IStartCommandService
{
    private readonly TelegramBotClient _botClient;
    private readonly IOptions<BotConfiguration> _options;

    public StartCommandService(IOptions<BotConfiguration> options)
    {
        _botClient = new TelegramBotClient(options.Value.Token);
        _options = options;
    }

    public async Task SetCommandsAsync()
    {
        try
        {
            // 1. Очищаем глобальные команды (чтобы не отображались в группах)
            await _botClient.SetMyCommands(Array.Empty<BotCommand>());

            // 2. Устанавливаем команды для ВСЕХ личных чатов
            var userCommands = new[]
            {
                new BotCommand("start", "Запустить бота"),
                // new BotCommand("help", "Как заполнять таблицу"),
            };
        
            await _botClient.SetMyCommands(
                commands: userCommands,
                scope: new BotCommandScopeAllPrivateChats()
            );

            // Команды для администраторов
            var adminCommands = new List<BotCommand>
            {
                // new() { Command = "start", Description = "Запустить бота" },
                // new() { Command = "help", Description = "Помощь" },
                new() { Command = "sendmessage", Description = "Отправить рассылку" },
                new() { Command = "getstatisticbychatid", Description = "Статистика по чату" },
                new() { Command = "getstatisticbyuserid", Description = "Статистика по пользователю" },
                new() { Command = "setweeklymessage", Description = "Установить еженедельное сообщение подписчикам" },
                new() { Command = "setdailymessage", Description = "Установить ежедневное сообщение для группы" },
            };

            foreach (var adminId in _options.Value.AdminIds)
            {
                try
                {
                    await _botClient.SetMyCommands(
                        commands: adminCommands,
                        scope: new BotCommandScopeChat() {ChatId = adminId} // Только для этого пользователя
                    );
                    Console.WriteLine($"✅ Админ-команды установлены для пользователя {adminId}");
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400) // Bad Request
                {
                    Console.WriteLine($"⚠️ Не удалось установить команды для {adminId}. Проверьте:");
                    Console.WriteLine("1. Бот должен быть запущен пользователем (личный чат открыт)");
                    Console.WriteLine("2. Пользователь не заблокировал бота");
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка для админа {adminId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting bot commands: {ex.Message}");
        }
    }
}