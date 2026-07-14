using ContainerFather.Bot.Services.Dto;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.States;
using ContainerFather.Core.Enums;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ContainerFather.Bot.Services.TelegramBot;

public class BroadcastService : IBroadcastService
{
    private readonly Dictionary<long, BroadcastSession> _sessions = new();
    private readonly IChatRepository _chatRepository;
    private readonly ITelegramBotClient _botClient;
    private readonly IBroadcastMessageRepository _broadcastMessageRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BroadcastService> _logger;
    private readonly IOptions<BotConfiguration> _options;

    public BroadcastService(
        IChatRepository chatRepository,
        IUserRepository userRepository,
        IOptions<BotConfiguration> options,
        IBroadcastMessageRepository broadcastMessageRepository,
        ILogger<BroadcastService> logger)
    {
        _options = options;
        _userRepository = userRepository;
        _broadcastMessageRepository = broadcastMessageRepository;
        _chatRepository = chatRepository;
        var clientOptions = new TelegramBotClientOptions(options.Value.TelegramToken);
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(360) };
        _botClient = new TelegramBotClient(clientOptions, httpClient);
        _logger = logger;
    }

    public async Task StartBroadcastSessionAsync(long userId)
    {
        var chats = await _chatRepository.GetChatLists(CancellationToken.None);

        _sessions[userId] = new BroadcastSession
        {
            UserId = userId,
            State = BroadcastState.WaitingForChatSelection
        };

        var keyboard = CreateChatSelectionKeyboard(chats);

        try
        {
            await _botClient.SendMessage(
                chatId: userId,
                text: "📢 Выберите чат для рассылки:",
                replyMarkup: keyboard
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task EnterMessage(long userId)
    {
        if (_sessions.TryGetValue(userId, out var session))
        {
            session.SelectedChatId = null;
            session.SelectedChatName = null;
            session.State = BroadcastState.WaitingForMessageTextForAll;

            await _botClient.SendMessage(
                chatId: userId,
                text: $"✅ Выбраны все чаты\n\n📝 Введите текст сообщения для рассылки:",
                replyMarkup: new ReplyKeyboardRemove()
            );
        }
    }

    public async Task SelectChatAsync(long userId, long chatId, string chatName)
    {
        if (_sessions.TryGetValue(userId, out var session))
        {
            session.SelectedChatId = chatId;
            session.SelectedChatName = chatName;
            session.State = BroadcastState.WaitingForMessageText;

            await _botClient.SendMessage(
                chatId: userId,
                text: $"✅ Выбран чат: {chatName}\n\n📝 Введите текст сообщения для рассылки:",
                replyMarkup: new ReplyKeyboardRemove()
            );
        }
    }

    public async Task SendBroadcastMessageForAllAsync(long userId, string messageText)
    {
        if (!_sessions.TryGetValue(userId, out var session) || session.SelectedChatId != null)
        {
            await _botClient.SendMessage(userId,
                "❌ Сессия рассылки не найдена. Начните заново с /sendMessage");
            return;
        }

        try
        {
            var memberIds = await _userRepository.GetUserList(new GetUserListRequest
            {
                UserType = UserType.BotUser,
                OnlyActive = true
            }, CancellationToken.None);
            
            var sentCount = 0;
            var failedCount = 0;

            // Отправка сообщения о начале рассылки
            await _botClient.SendMessage(
                userId,
                $"🚀 Начинаем рассылку в чат {session.SelectedChatName}...\nПолучателей: {memberIds.Count}"
            );


            // Рассылка каждому пользователю
            foreach (var member in memberIds)
            {
                try
                {
                    await _botClient.SendMessage(
                        member.TelegramId,
                        text: messageText,
                        disableNotification: false
                    );
                    sentCount++;
                    await Task.Delay(50); // Задержка чтобы не превысить лимиты Telegram
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить сообщение пользователю {UserId}", member.TelegramId);
                    failedCount++;
                }
            }

            // Отчет о результатах
            await _botClient.SendMessage(
                userId,
                $"📊 Рассылка завершена!\n\n" +
                $"✅ Успешно: {sentCount}\n" +
                $"❌ Ошибок: {failedCount}\n"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении рассылки");
            await _botClient.SendMessage(userId, "❌ Произошла ошибка при рассылке");
        }
        finally
        {
            _sessions.Remove(userId);
        }
    }

    public async Task ProcessBroadcastMessageAsync(long userId, string messageText)
    {
        if (!_sessions.TryGetValue(userId, out var session) || session.SelectedChatId == null)
        {
            await _botClient.SendMessage(userId,
                "❌ Сессия рассылки не найдена. Начните заново с /sendMessage");
            return;
        }

        try
        {
            var memberIds = await _chatRepository.GetChatMembers(session.SelectedChatId.Value, CancellationToken.None);
            var sentCount = 0;
            var failedCount = 0;

            // Отправка сообщения о начале рассылки
            await _botClient.SendMessage(
                userId,
                $"🚀 Начинаем рассылку в чат {session.SelectedChatName}...\nПолучателей: {memberIds.Count}"
            );
            
            // Рассылка каждому пользователю
            foreach (var member in memberIds)
            {
                try
                {
                    await _botClient.SendMessage(
                        member.UserTelegramId,
                        text: messageText,
                        disableNotification: false
                    );
                    sentCount++;
                    await Task.Delay(50); // Задержка чтобы не превысить лимиты Telegram
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить сообщение пользователю {UserId}", member.UserTelegramId);
                    failedCount++;
                }
            }

            // Отчет о результатах
            await _botClient.SendMessage(
                userId,
                $"📊 Рассылка завершена!\n\n" +
                $"✅ Успешно: {sentCount}\n" +
                $"❌ Ошибок: {failedCount}\n" +
                $"💬 Чат: {session.SelectedChatName}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении рассылки");
            await _botClient.SendMessage(userId, "❌ Произошла ошибка при рассылке");
        }
        finally
        {
            _sessions.Remove(userId);
        }
    }

    public async Task CancelBroadcastSessionAsync(long userId)
    {
        _sessions.Remove(userId);
        await _botClient.SendMessage(
            userId,
            "❌ Рассылка отменена",
            replyMarkup: new ReplyKeyboardRemove()
        );
    }

    public BroadcastSession? GetSession(long userId)
    {
        return _sessions.TryGetValue(userId, out var session) ? session : null;
    }

    private InlineKeyboardMarkup CreateChatSelectionKeyboard(List<GetChatListResponse> chats)
    {
        var buttons = chats.Select(chat =>
            new[] { InlineKeyboardButton.WithCallbackData(chat.ChatName, $"broadcast_chat {chat.ChatId}") }
        ).ToList();
        
        // добавляю кнопку отправить всем подписчикам
        buttons.Add(new [] {InlineKeyboardButton.WithCallbackData("Отправить всем подписчикам бота", "broadcast_all")});
        
        // Добавляем кнопку отмены
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "broadcast_cancel") });

        return new InlineKeyboardMarkup(buttons);
    }

    public async Task SendWeeklyBroadcastMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        var message =  await _broadcastMessageRepository.GetActiveBroadcastMessage(BroadcastMessagePeriodType.Weekly, cancellationToken);
        if (message == null)
        {
            Console.WriteLine("Еженеделная рассылка отменена пока не создано сообщение для нее");
            return;
        }
        
        var userList = await _userRepository.GetUserListByChatId(chatId, cancellationToken);
        foreach (var user in userList)
        {
            try
            {
                await _botClient.SendMessage(
                    user.TelegramId,
                    text: message.Message,
                    disableNotification: true
                );
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить сообщение пользователю {UserId}", user.TelegramId);
            }
        }
    }
    
    public async Task SendDailyBroadcastMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        var message =  await _broadcastMessageRepository.GetActiveBroadcastMessage(BroadcastMessagePeriodType.Daily, cancellationToken);
        if (message == null)
        {
            Console.WriteLine("Ежедневная рассылка отменена пока не создано сообщение для нее");
            return;
        }
        var chat = await _chatRepository.GetChatById(chatId, cancellationToken);

        await _botClient.SendMessage(
            chatId: chat.TelegramId,
            text: message.Message,
            disableNotification: false,
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("Биржа контейнеров в Max", "https://max.ru/join/6V2LpcbRPjbb1a5_sPclvd781XFjG18IIA-bL7NM4PM") },
                new[] { InlineKeyboardButton.WithUrl("Есть груз/пустой", "https://t.me/+0dWxrmVrUellOTYy") },
                new[] { InlineKeyboardButton.WithUrl("Нужен груз/машина в Max", "https://max.ru/join/JCUpKTeABgVlJyTNhF_fkkrmceQ3-NLqTX5tpL4kmdQ") }
            })
        );
    }
    
    public async Task SendDailyChanelBroadcastMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        var message =  await _broadcastMessageRepository.GetActiveBroadcastMessage(BroadcastMessagePeriodType.DailyChanel, cancellationToken);
        if (message == null)
        {
            Console.WriteLine("Ежедневная рассылка отменена пока не создано сообщение для нее");
            return;
        }
        var chat = await _chatRepository.GetChatById(chatId, cancellationToken);

        await _botClient.SendMessage(
            chatId: chat.TelegramId,
            text: message.Message,
            parseMode: ParseMode.Html,
            disableNotification: false,
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithUrl("Нужен груз/машина в Max", "https://max.ru/join/JCUpKTeABgVlJyTNhF_fkkrmceQ3-NLqTX5tpL4kmdQ") }
            })
        );
    }
}