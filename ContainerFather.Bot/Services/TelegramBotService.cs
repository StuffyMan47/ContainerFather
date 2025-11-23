using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ContainerFather.Bot.Services.Dto;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.States;
using ContainerFather.Core.Enums;
using ContainerFather.Core.Interfaces.Settings;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.BroadcastMessages.Models;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Chats.Models;
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ContainerFather.Bot.Services;

public class TelegramBotService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IBroadcastMessageRepository _broadcastMessageRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IAdminDialogService _adminDialogService;
    private readonly IGetStatisticHandler _getStatisticHandler;
    private readonly IBroadcastService _broadcastService;
    private readonly BotConfiguration _botConfiguration;
    private readonly TelegramBotClient _botClient;

    public TelegramBotService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IBroadcastMessageRepository broadcastMessageRepository,
        IChatRepository chatRepository,
        IAdminDialogService adminDialogService,
        IGetStatisticHandler getStatisticHandler,
        IBroadcastService broadcastService,
        IOptions<BotConfiguration> options)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _broadcastMessageRepository = broadcastMessageRepository ??
                                      throw new ArgumentNullException(nameof(broadcastMessageRepository));
        _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        _adminDialogService = adminDialogService ?? throw new ArgumentNullException(nameof(adminDialogService));
        _getStatisticHandler = getStatisticHandler ?? throw new ArgumentNullException(nameof(getStatisticHandler));
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _botConfiguration = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _botClient = new TelegramBotClient(options.Value.Token);
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.From != null &&
            _botConfiguration.AdminIds.Contains(update.Message.From.Id) &&
            _adminDialogService.IsInDialog(update.Message.From.Id))
        {
            var adminId = update.Message.From.Id;
            var currentState = _adminDialogService.GetDialogState(adminId);

            switch (currentState)
            {
                case AdminDialogState.ManagingWeeklyMessage:
                    await HandleWeeklyMessageActionAsync(update.Message, cancellationToken);
                    return;

                case AdminDialogState.WaitingForNewWeeklyMessage:
                    await HandleNewWeeklyMessageInputAsync(adminId, update.Message, cancellationToken);
                    return;

                case AdminDialogState.ManagingDailyMessage:
                    await HandleDailyMessageActionAsync(update.Message, cancellationToken);
                    return;

                case AdminDialogState.WaitingForNewDailyMessage:
                    await HandleNewDailyMessageInputAsync(adminId, update.Message, cancellationToken);
                    return;

                // Другие состояния...
            }
        }

        if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Document)
        {
            var fileId = update.Message.Document.FileId;
            await using var ms = new MemoryStream();
            var tgFile = await _botClient.GetInfoAndDownloadFile(fileId, ms);

            var result = update.Message.Document.FileName!.ToLower() switch
            {
                var name when name.EndsWith(".xlsx") => ParseExcel(ms, update.Message.From.Username),
                _ => throw new Exception("Неподдерживаемый формат файла. Поддерживаются только CSV и XLSX.")
            };

            if (result.Any())
            {
                await WriteToGoogleSheets(result);
                await _botClient.SendMessage(update.Message.Chat.Id,
                    $"Данные записаны в Google Sheets", cancellationToken: cancellationToken);
            }
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackData = update.CallbackQuery.Data;
            var buttonInfo = update.CallbackQuery.Data.Split(' ');

            // Обработка callback для рассылки
            if (callbackData.StartsWith("broadcast_chat"))
            {
                var groupId = long.Parse(buttonInfo[1]);
                var chat = await _chatRepository.GetChatById(groupId, CancellationToken.None);
                if (chat != null)
                {
                    await _broadcastService.SelectChatAsync(update.CallbackQuery.From.Id, chat.Id, chat.Name);
                }

                await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }
            else if (callbackData == "broadcast_cancel")
            {
                await _broadcastService.CancelBroadcastSessionAsync(update.CallbackQuery.From.Id);
                await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            switch (buttonInfo[0])
            {
                case "user":
                {
                    await _getStatisticHandler.SendUserStatistic(Int64.Parse(buttonInfo[1]),
                        update.CallbackQuery.Message.Chat.Id, cancellationToken);
                    break;
                }
                case "chat":
                {
                    await _getStatisticHandler.SendChatStatistic(Int64.Parse(buttonInfo[1]),
                        update.CallbackQuery.Message.Chat.Id,
                        cancellationToken);
                    break;
                }
            }
        }

        if (update.Message is not { } message)
            return;

        var telegramUserId = message.From?.Id;
        var text = message.Text ?? string.Empty;

        if (telegramUserId == null) return;

        if (message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            var chatId = await _chatRepository.GetOrCreateChat(message.Chat.Id, message.Chat.Title ?? "no name group",
                cancellationToken);
            var userId = await SaveOrUpdateUserAsync(message.From!, chatId, cancellationToken);

            await _messageRepository.SaveMessageAsync(userId, text, chatId);
        }
        else
        {
            var userId = await SaveOrUpdateUserAsync(message.From!, null, cancellationToken);
        }

        if (message.Chat.Type is ChatType.Private)
        {
            var adminIds = _botConfiguration.AdminIds;
            if (adminIds.Contains((long)message.From?.Id))
            {
                // Проверяем активную сессию рассылки
                var broadcastSession = _broadcastService.GetSession((long)message.From?.Id);
                if (broadcastSession?.State == BroadcastState.WaitingForMessageText)
                {
                    await _broadcastService.ProcessBroadcastMessageAsync((long)message.From?.Id, text);
                    return;
                }

                await HandleAdminCommandAsync(message, text, cancellationToken);
            }
            else
            {
                await HandleCommandAsync(message, text, cancellationToken);
            }
        }
    }

    private async Task<long> SaveOrUpdateUserAsync(User user, long? chatId, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByTelegramIdAsync(user.Id, cancellationToken);
        long userId;

        if (existingUser == null)
        {
            var newUser = new CreateUserRequest
            {
                TelegramId = user.Id,
                Username = user.Username,
            };
            userId = await _userRepository.CreateUser(newUser, cancellationToken);
        }
        else
        {
            userId = existingUser.Id;
        }

        if (chatId != null && ((existingUser != null && !existingUser.ChatIds.Contains(chatId.Value)) ||
                               existingUser == null))
        {
            await _chatRepository.ConnectUserToChat(userId, chatId.Value);
        }

        return userId;
    }

    private async Task HandleAdminCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        var adminIds = _botConfiguration.AdminIds;

        if (!adminIds.Contains((long)message.From?.Id))
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "Команда доступна только администратору",
                cancellationToken: cancellationToken);
            return;
        }

        switch (command.ToLower())
        {
            case "/start":
                await SaveOrUpdateUserAsync(message.From!, null, cancellationToken);
                break;
            case "/help":
                await SendHelpMessage(message, cancellationToken);
                break;
            case "/sendmessage": //команда для интерактивной рассылки
                await _broadcastService.StartBroadcastSessionAsync((long)message.From?.Id);
                break;
            case "/getstatisticbychatid": //команда для получения статистики по выбранному чату
                await HandleGetStatisticByChatIdCommandAsync(message, cancellationToken);
                break;
            case "/getstatisticbyuserid": //команда для получения статистики по пользователю
                await HandleGetStatisticByUserIdCommandAsync(message, cancellationToken);
                break;
            case "/setweeklymessage":
                await HandleSetWeeklyMessageCommandAsync(message, cancellationToken);
                break;
            case "/setdailymessage":
                await HandleSetDailyMessageCommandAsync(message, cancellationToken);
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

    private async Task HandleCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        switch (command.ToLower())
        {
            case "/start":
                await SaveOrUpdateUserAsync(message.From!, null, cancellationToken);
                break;
            case "/help":
                await SendHelpMessage(message, cancellationToken);
                break;
        }
    }

    private async Task SendHelpMessage(Message message, CancellationToken cancellationToken)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "С помощью этого бота вы можете загрузить свои предложения о продаже контейнеров для наших администраторов. " +
            "Для этого отправьте Excel документ заполненный по шаблону в бота");

        string filePath = Path.Combine(Environment.CurrentDirectory, "Files", "Example.xlsx");

        // Проверяем существование файла
        if (!File.Exists(filePath))
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "ℹ️ Подробная инструкция временно недоступна. " +
                "Обратитесь к администратору для получения руководства.",
                cancellationToken: cancellationToken
            );
            return;
        }

        // Отправляем документ
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        await _botClient.SendDocument(
            chatId: message.Chat.Id,
            document: new InputFileStream(fileStream, Path.GetFileName(filePath)),
            caption: "📎 Образец предложений о продаже контейнеров",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    private async Task SendBroadcastInstructionsAsync(long chatId)
    {
        await _botClient.SendMessage(
            chatId,
            "Для рассылки сообщения всем пользователям используйте команду:\n" +
            "/broadcast ваш_текст_сообщения");
    }

    private async Task HandleBroadcastAsync(Message message, string broadcastText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(broadcastText))
        {
            await _botClient.SendMessage(message.Chat.Id, "Текст рассылки не может быть пустым.");
            return;
        }

        var users = await _userRepository.GetUserList(new GetUserListRequest
        {
            OnlyActive = true
        }, cancellationToken);
        var successCount = 0;

        foreach (var user in users)
        {
            try
            {
                await _botClient.SendMessage(user.TelegramId, broadcastText);
                successCount++;
                await Task.Delay(50); // Задержка чтобы не превысить лимиты Telegram
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но продолжаем рассылку
                Console.WriteLine($"Ошибка отправки пользователю {user.TelegramId}: {ex.Message}");
            }
        }

        await _botClient.SendMessage(
            message.Chat.Id,
            $"Рассылка завершена. Успешно отправлено: {successCount} из {users.Count} пользователей.");
    }

    private async Task HandleSetWeeklyMessageAsync(global::Telegram.Bot.Types.Message message, string weeklyMessage)
    {
        if (string.IsNullOrWhiteSpace(weeklyMessage))
        {
            await _botClient.SendMessage(message.Chat.Id, "Текст еженедельного сообщения не может быть пустым.");
            return;
        }

        var broadcastMessage = new CreateBroadcastMessageRequest()
        {
            Message = weeklyMessage,
            PeriodType = BroadcastMessagePeriodType.Weekly
        };

        await _broadcastMessageRepository.CreateBroadcastMessage(broadcastMessage, CancellationToken.None);

        await _botClient.SendMessage(
            message.Chat.Id,
            "Еженедельное сообщение установлено!");
    }

    public async Task SendWeeklyBroadcastAsync(CancellationToken cancellationToken)
    {
        var weeklyMessage =
            await _broadcastMessageRepository.GetActiveBroadcastMessage(BroadcastMessagePeriodType.Weekly,
                cancellationToken);
        if (weeklyMessage == null) return;

        var users = await _userRepository.GetUserList(new GetUserListRequest
        {
            OnlyActive = true
        }, cancellationToken);

        foreach (var user in users)
        {
            try
            {
                await _botClient.SendMessage(user.TelegramId, weeklyMessage.Message);
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка еженедельной рассылки пользователю {user.TelegramId}: {ex.Message}");
            }
        }
    }

    private async Task HandleGetStatisticByChatIdCommandAsync(Message message, CancellationToken cancellationToken)
    {
        // Получаем список всех чатов
        var chats = await _chatRepository.GetChatLists(cancellationToken);

        if (!chats.Any())
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "В базе данных нет чатов для отображения.",
                cancellationToken: cancellationToken);
            return;
        }

        var chatList = "Список чатов:\n\n";
        foreach (var chat in chats.OrderBy(c => c.ChatName))
        {
            chatList += $"{chat.ChatName}\nID: {chat.ChatId}\n\n";
        }

        chatList += "Введите ID чата для получения статистики:";

        var buttons = SplitArray(chats.Select(x => new InlineKeyboardButton(x.ChatName, $"chat {x.ChatId}")).ToArray());

        await _botClient.SendMessage(
            message.Chat.Id,
            chatList,
            replyMarkup: buttons,
            cancellationToken: cancellationToken);
    }

    private async Task HandleGetStatisticByUserIdCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var adminId = message.From.Id;

        // Получаем список всех пользователей
        var users = await _userRepository.GetUserList(new GetUserListRequest()
        {
            OnlyActive = true
        }, cancellationToken);

        if (!users.Any())
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "В базе данных нет пользователей для отображения.",
                cancellationToken: cancellationToken);
            return;
        }

        var userList = "Список пользователей:\n\n";
        foreach (var user in users.OrderBy(u => u.Username))
        {
            var userName = $"{user.Username}";

            userList += $"{userName}\nID: {user.Id}\n\n";
        }

        userList += "Введите ID пользователя для получения статистики:";

        var buttons = SplitArray(users.Select(x => new InlineKeyboardButton(x.Username, $"user {x.Id}")).ToArray());
        await _botClient.SendMessage(
            message.Chat.Id,
            userList,
            replyMarkup: buttons,
            cancellationToken: cancellationToken);

        // adminDialogService.StartUserStatisticDialog(adminId);
    }

    public static T[][] SplitArray<T>(T[] sourceArray, int elementsPerRow = 3)
    {
        if (sourceArray == null || sourceArray.Length == 0)
            return Array.Empty<T[]>();

        var rows = (int)Math.Ceiling(sourceArray.Length / (double)elementsPerRow);
        var result = new T[rows][];

        for (int i = 0; i < rows; i++)
        {
            var startIndex = i * elementsPerRow;
            var elementsInThisRow = Math.Min(elementsPerRow, sourceArray.Length - startIndex);
            result[i] = new T[elementsInThisRow];

            Array.Copy(sourceArray, startIndex, result[i], 0, elementsInThisRow);
        }

        return result;
    }

    private async Task HandleSetWeeklyMessageCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var adminId = message.From.Id;

        // Получаем текущее сообщение
        var currentMessage = await _broadcastMessageRepository.GetActiveBroadcastMessage(
            BroadcastMessagePeriodType.Weekly,
            cancellationToken
        );

        var currentMessageText = currentMessage?.Message ?? "Не установлено";

        var response = $"Текущее еженедельное сообщение:\n\n{currentMessageText}\n\n";
        response += "Выберите действие:";

        // Одноразовая клавиатура
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Оставить как есть" },
            new KeyboardButton[] { "Изменить сообщение" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: response,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );

        _adminDialogService.SetDialogData(adminId, "CurrentMessage", currentMessageText);
        _adminDialogService.SetDialogState(adminId, AdminDialogState.ManagingWeeklyMessage);
    }

    private async Task HandleSetDailyMessageCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var adminId = message.From.Id;

        // Получаем текущее еженедельное сообщение
        var currentMessage =
            await _broadcastMessageRepository.GetActiveBroadcastMessage(BroadcastMessagePeriodType.Daily,
                cancellationToken);
        var currentMessageText = currentMessage?.Message ?? "Не установлено";

        var response = $"Текущее еженедельное сообщение:\n\n{currentMessageText}\n\n";
        response += "Выберите действие:";

        // Создаем клавиатуру с кнопками
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Оставить как есть"),
                new KeyboardButton("Изменить")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await _botClient.SendMessage(
            message.Chat.Id,
            response,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        _adminDialogService.StartDailyMessageDialog(adminId);
        _adminDialogService.SetDialogData(adminId, "CurrentMessage", currentMessageText);
    }

    private async Task HandleWeeklyMessageActionAsync(Message message,
        CancellationToken cancellationToken)
    {
        var adminId = message.From.Id;
        var action = message.Text;
        var currentMessage = _adminDialogService.GetDialogData<string>(adminId, "CurrentMessage");

        if (action == "Оставить как есть")
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✅ Еженедельное сообщение осталось без изменений",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );
            _adminDialogService.CompleteDialog(adminId);
            return;
        }

        if (action == "Изменить сообщение")
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✏️ Введите новое текст сообщения (максимум 4096 символов):",
                replyMarkup: new ReplyKeyboardRemove(), // Убираем клавиатуру
                cancellationToken: cancellationToken
            );

            // Переходим к следующему состоянию
            _adminDialogService.SetDialogState(adminId, AdminDialogState.WaitingForNewWeeklyMessage);
            return;
        }

        _adminDialogService.SetDialogState(adminId, AdminDialogState.None);
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "❌ Неизвестное действие. Пожалуйста, выберите из предложенных вариантов.",
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleNewWeeklyMessageInputAsync(long adminId, Message message,
        CancellationToken cancellationToken)
    {
        var newMessage = message.Text;

        if (string.IsNullOrWhiteSpace(newMessage))
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "Сообщение не может быть пустым. Введите новое еженедельное сообщение:",
                cancellationToken: cancellationToken);
            return;
        }

        // Сохраняем новое сообщение
        var broadcastMessage = new CreateBroadcastMessageRequest
        {
            Message = newMessage,
            PeriodType = BroadcastMessagePeriodType.Weekly
        };

        await _broadcastMessageRepository.CreateBroadcastMessage(broadcastMessage, cancellationToken);

        await _botClient.SendMessage(
            message.Chat.Id,
            "Еженедельное сообщение успешно обновлено.",
            cancellationToken: cancellationToken);

        _adminDialogService.CompleteDialog(adminId);
    }

    private async Task HandleDailyMessageActionAsync(Message message,
        CancellationToken cancellationToken)
    {
        var adminId = message.From.Id;
        var action = message.Text;
        var currentMessage = _adminDialogService.GetDialogData<string>(adminId, "CurrentMessage");

        if (action == "Оставить как есть")
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✅ Ежедневное сообщение осталось без изменений",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );
            _adminDialogService.CompleteDialog(adminId);
            return;
        }

        if (action == "Изменить")
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✏️ Введите новое текст сообщения (максимум 4096 символов):",
                replyMarkup: new ReplyKeyboardRemove(), // Убираем клавиатуру
                cancellationToken: cancellationToken
            );

            // Переходим к следующему состоянию
            _adminDialogService.SetDialogState(adminId, AdminDialogState.WaitingForNewDailyMessage);
            return;
        }

        _adminDialogService.SetDialogState(adminId, AdminDialogState.None);
        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "❌ Неизвестное действие. Пожалуйста, выберите из предложенных вариантов.",
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleNewDailyMessageInputAsync(long adminId, Message message,
        CancellationToken cancellationToken)
    {
        var newMessage = message.Text;

        if (string.IsNullOrWhiteSpace(newMessage))
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "Сообщение не может быть пустым. Введите новое еженедельное сообщение:",
                cancellationToken: cancellationToken);
            return;
        }

        // Сохраняем новое сообщение
        var broadcastMessage = new CreateBroadcastMessageRequest
        {
            Message = newMessage,
            PeriodType = BroadcastMessagePeriodType.Daily
        };

        await _broadcastMessageRepository.CreateBroadcastMessage(broadcastMessage, cancellationToken);

        await _botClient.SendMessage(
            message.Chat.Id,
            "Ежедневное сообщение успешно обновлено.",
            cancellationToken: cancellationToken);

        _adminDialogService.CompleteDialog(adminId);
    }

    private List<ContainerRequestModel> ParseExcel(Stream stream, string username)
    {
        var result = new List<ContainerRequestModel>();

        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Пропускаем заголовок

            var rowNumber = 2; // Начинаем с строки 2 (после заголовка)
            foreach (var row in rows)
            {
                var model = new ContainerRequestModel
                {
                    Size = row.Cell(1).Value.ToString()?.Trim() ?? "", // A
                    Type = row.Cell(2).Value.ToString()?.Trim() ?? "", // B
                    Condition = row.Cell(3).Value.ToString()?.Trim() ?? "", // C
                    City = row.Cell(4).Value.ToString()?.Trim() ?? "", // D
                    Availability = row.Cell(5).Value.ToString()?.Trim() ?? "", // E
                    PriceWithTax = decimal.Parse(row.Cell(6).Value.ToString()?.Trim() ?? String.Empty), // D
                    PriceWithoutTax = decimal.Parse(row.Cell(7).Value.ToString()?.Trim() ?? String.Empty), // G
                    Currency = row.Cell(8).Value.ToString()?.Trim() ?? "", // H
                    TransactionType = row.Cell(9).Value.ToString()?.Trim() ?? "", // I
                    Date = DateTimeOffset.UtcNow,
                    Username = $"@{username}"
                };

                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(model.Size) ||
                    string.IsNullOrWhiteSpace(model.Type) ||
                    string.IsNullOrWhiteSpace(model.City) ||
                    string.IsNullOrWhiteSpace(model.Username))
                {
                    rowNumber++;
                    continue;
                }

                result.Add(model);


                rowNumber++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return result;
    }

    private async Task WriteToGoogleSheets(List<ContainerRequestModel> models)
    {
        var credential = GoogleCredential.FromJson(_botConfiguration.GoogleAuth.Key)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        // Пример инициализации сервиса (в конструкторе)
        var sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential, // Ваше GoogleCredential
            ApplicationName = "ContainerFather.Bot",
        });
        var spreadsheetId = "1Q4aHnNPNFXxlwTxRNJk9IUf1m6V2wWV1HTc3rnu-ZbE"; // ID таблицы из URL
        // Проверка входных данных
        if (models == null || models.Count == 0)
        {
            Console.WriteLine("Попытка записи пустого списка данных в Google Таблицу");
            return;
        }

        try
        {
            // Преобразуем модели в данные для Google Sheets
            var values = new List<IList<object>>();

            foreach (var model in models)
            {
                // Безопасное форматирование даты с обработкой null
                string formattedDate = model.Date.ToString("dd.MM.yyyy HH:mm");

                var row = new List<object>
                {
                    model.Size,
                    model.Type,
                    model.Condition ?? string.Empty,
                    model.City,
                    formattedDate,
                    model.Username?.Trim() ?? string.Empty,
                    model.Availability ?? string.Empty,
                    model.PriceWithTax.HasValue
                        ? model.PriceWithTax.Value
                        : string.Empty, // Форматирование цены как валюты
                    model.PriceWithoutTax.HasValue ? model.PriceWithoutTax.Value : string.Empty,
                    model.Currency,
                    model.TransactionType,
                };
                values.Add(row);
            }

            // Динамическое определение диапазона
            var range = $"A2:K{values.Count + 1}";

            var valueRange = new ValueRange
            {
                Values = values
            };

            // 3. Используем Append вместо Update
            var appendRequest = sheetsService.Spreadsheets.Values.Append(
                valueRange,
                spreadsheetId,
                range
            );

            // Важные настройки для корректной работы
            appendRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.InsertDataOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum
                    .INSERTROWS; // ← Добавлять новые строки
            appendRequest.IncludeValuesInResponse = true; // Для получения информации о результате

            // Выполнение запроса с таймаутом
            var response = await appendRequest.ExecuteAsync();

            Console.WriteLine($"Успешно записано {response.Updates.UpdatedRows} строк в Google Таблицу");
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 403)
        {
            Console.WriteLine("Ошибка доступа к Google Таблице: Проверьте права доступа сервисного аккаунта");
            throw new ApplicationException("Недостаточно прав для записи в таблицу", ex);
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            Console.WriteLine($"Таблица с ID {spreadsheetId} не найдена");
            throw new ApplicationException("Целевая таблица не существует", ex);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine("Таймаут операции записи в Google Таблицу");
            throw new TimeoutException("Превышено время ожидания ответа от Google API", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при записи в Google Таблицу: {ex.Message}");
            throw;
        }
    }
}