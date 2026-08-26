using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContainerFather.Bot.AiTunnelService;
using ContainerFather.Bot.AiTunnelService.Model;
using ContainerFather.Bot.Handlers;
using ContainerFather.Bot.Helpers;
using ContainerFather.Bot.Services.Dto;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.SiteService;
using ContainerFather.Bot.States;
using ContainerFather.Core.Enums;
using ContainerFather.Core.Enums.SiteEnums;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.BroadcastMessages.Models;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Containers.Interfaces;
using ContainerFather.Core.UseCases.Containers.Models;
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using ContainerFather.Core.UseCases.Users.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Max.Bot;
using Max.Bot.Configuration;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;

namespace ContainerFather.Bot.Services.MaxBot;

public class MaxBotService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IBroadcastMessageRepository _broadcastMessageRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IAdminDialogService _adminDialogService;
    private readonly IGetStatisticHandler _getStatisticHandler;
    private readonly IBroadcastService _broadcastService;
    private readonly BotConfiguration _botConfiguration;
    private readonly MaxClient _maxBotClient;
    private readonly IWebHostEnvironment _environment;
    private readonly IAiTunnelClient _aiTunnelClient;
    private readonly ISiteClient _siteClient;
    private readonly IContainerRepository _containerRepository;
    private readonly ISitePostingService _sitePostingService;

    public MaxBotService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IBroadcastMessageRepository broadcastMessageRepository,
        IChatRepository chatRepository,
        IAdminDialogService adminDialogService,
        IGetStatisticHandler getStatisticHandler,
        IBroadcastService broadcastService,
        IWebHostEnvironment environment,
        IAiTunnelClient aiTunnelClient,
        ISiteClient siteClient,
        ISitePostingService sitePostingService,
        IContainerRepository containerRepository,
        MaxClient maxBotClient,
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
        _maxBotClient = maxBotClient ?? throw new ArgumentNullException(nameof(maxBotClient));
        _environment = environment;
        _aiTunnelClient = aiTunnelClient ?? throw new ArgumentNullException(nameof(aiTunnelClient));
        _siteClient = siteClient ?? throw new ArgumentNullException(nameof(siteClient));
        _sitePostingService = sitePostingService ?? throw new ArgumentNullException(nameof(sitePostingService));
        _containerRepository = containerRepository ?? throw new ArgumentNullException(nameof(containerRepository));
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message)
                return;

            var telegramUserId = message.Sender?.Id;
            var text = message.Text ?? string.Empty;

            if (telegramUserId == null) return;

            // Обработка сообщений в чатах
            if (update.Message.Recipient?.ChatType?.Equals("chat") ?? false)
            {
                var chatId = await _chatRepository.GetOrCreateChat(update.Message.Recipient?.ChatId ?? 0,
                    "no name group",
                    MessengerType.Max,
                    cancellationToken);
                var userId = await SaveOrUpdateUserAsync(message.Sender!, chatId, cancellationToken);

                
                // bool HasTextLinkEntities(Message message) => 
                //     message.Entities?.Any(x => x.Type == MessageEntityType.TextLink) ?? false;

                if (ContainsLink(message.Text))
                {
                    try
                    {
                        //пересылка сообщения с ссылкой
                        await _maxBotClient.Messages.ForwardMessageAsync(
                            // chatId: 1037799385,
                            messageId: message.Mid,
                            userId: 182677680,
                            cancellationToken: cancellationToken);
                        
                        await _maxBotClient.Messages.DeleteMessageAsync(
                            messageId: message.Mid,
                            cancellationToken: cancellationToken);

                        // Можно отправить предупреждение пользователю
                        await _maxBotClient.Messages.SendMessageAsync(
                            chatId: message.Recipient.ChatId.Value,
                            text: $"Запрещено отправлять ссылки в чате",
                            cancellationToken: cancellationToken);

                        return; // Прерываем дальнейшую обработку
                    }
                    catch (Exception ex)
                    {
                        // Бот не имеет прав на удаление сообщений
                        // Можно отправить сообщение в лог или админу
                        await _maxBotClient.Messages.SendMessageAsync(
                            chatId: 244266512,
                            text: "Ошибка при удалении сообщения" +
                            $"{ex.Message}\n" +
                            // $"update: {update.Message?.Username ?? "непонятно кого"}, update type : {update.Type}" +
                            $"было написано {update.Message?.Text}",
                            cancellationToken: cancellationToken);
                    }
                }
                
                //обработка через ИИ и запись в excel
                List<string> stopWords = ["терминал", "куплю", "ищу", "предложите"];
                if (!string.IsNullOrEmpty(text) &&
                    stopWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    //покупка
                }
                else if (!string.IsNullOrEmpty(text) &&
                         update.Message.Recipient?.ChatId == -69316722843808)//-76706245251088)
                {
                    await HandleMessage(update, message, cancellationToken);
                }

                await _messageRepository.SaveMessageAsync(userId, text, chatId);
            }
            else
            {
                var userId = await SaveOrUpdateUserAsync(message.Sender!, null, cancellationToken, true);
            }
        }
        catch (Exception ex)
        {
            await _maxBotClient.Messages.SendMessageAsync(
                244266512,
                "Возникла ошибка при обработке запроса:" +
                $"{ex.Message}\n" +
                $"update: {update.Message?.Sender?.Username ?? "непонятно кого"}, update type : {update.Type}" +
                $"было написано {update.Message?.Text}",
                cancellationToken: cancellationToken);
        }
    }

    private bool ContainsLink(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Регулярное выражение для поиска URL
        var urlPattern = @"(https?://|www\.)[^\s]+";
        return Regex.IsMatch(text, urlPattern, RegexOptions.IgnoreCase);
    }

    private static long GenerateArticleId()
    {
        // Generates an 8-9 digit number using time components and a small random part
        var now = DateTime.UtcNow;
        var timePart = (now.DayOfYear * 100000) + (now.Hour * 1000) + (now.Minute * 10) + (now.Second % 10);
        var randomPart = new Random().Next(10, 99);
        return long.Parse($"{timePart}{randomPart}");
    }

    public async Task HandleMessage(Update update, Message message, CancellationToken cancellationToken)
    {
        List<AiContainerResponse> objects = new List<AiContainerResponse>();
        string result = null;
        try
        {
            result = await _aiTunnelClient.SendMessage(message.Text);
            objects = JsonSerializer.Deserialize<List<AiContainerResponse>>(result);
        }
        catch (JsonException ex)
        {
            if (result == null || !result.Contains("Амбасадор"))
            {
                await _maxBotClient.Messages.SendMessageAsync(
                    244266512,
                    "Ошибка при десериализации сообщения из ai tunnel в Максе:" +
                    $"{ex.Message}\n" +
                    $"username: {message?.Sender?.Username}\n" +
                    $"message: {message?.Text}",
                    cancellationToken: cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            await _maxBotClient.Messages.SendMessageAsync(
                244266512,
                "Ошибка при обработке сообщения в ai tunnel в Максе:" +
                $"{ex.Message}\n" +
                $"username: {message?.Sender?.Username}\n" +
                $"message: {message?.Text}",
                cancellationToken: cancellationToken);
            throw;
        }

        var allMissingFields = new HashSet<string>();
        bool hasInvalidRecords = false;

        foreach (var obj in objects)
        {
            var missingFields = GetMissingFields(obj);
            if (missingFields.Any())
            {
                hasInvalidRecords = true;
                foreach (var field in missingFields)
                {
                    allMissingFields.Add(field);
                }
            }
        }

        // Если есть неполные записи - запрашиваем дополнение информации
        if (hasInvalidRecords)
        {
            // Формируем читаемый список отсутствующих полей
            var missingFieldsList = string.Join(", ", allMissingFields);
            var responseText =
                $"Дополните Ваше предложение необходимой информацией ({missingFieldsList}) Так выйдем на сделку быстрее";

            await _maxBotClient.Messages.ReplyToMessageAsync(
                chatId: update.ChatId,
                messageId: update.Message?.Mid ?? "",
                text: responseText,
                cancellationToken: cancellationToken);
            // Не записываем в таблицу при неполных данных
            return;
        }

        try
        {
            if (objects != null && objects.Count > 0)
            {
                List<ContainerRequestModel> containers = [];
                foreach (var x in objects)
                {
                    var sourceId = Guid.NewGuid();
                    var articleId = GenerateArticleId();
                    var city = CityGeoService.GetCityCoordinatesAsync(x.City);
                    var container = new ContainerRequestModel
                    {
                        SourceId = sourceId,
                        ArticleId = articleId,
                        ConditionId = x.ConditionId ?? ConditionEnum.Cw,
                        CategoryId = x.CategoryId,
                        Availability = x.Availability,
                        City = x.City,
                        Latitude = city.Latitude.Value,
                        Longitude = city.Longitude.Value,
                        Date = DateTimeOffset.UtcNow,
                        Condition = x.ConditionName,
                        Currency = x.Currency.GetDescription(),
                        PriceWithoutTax = x.PriceWithoutTax,
                        PriceWithTax = x.PriceWithTax,
                        Size = x.Size,
                        Type = x.Type,
                        Count = x.Count,
                        Username = $"@{message.Sender?.Username ?? message.Sender?.FirstName}",
                        CurrencyId = x.Currency,
                        MessageUrl = ""
                    };
                    containers.Add(container);
                }
                var userInfo = await _userRepository.GetByTelegramIdAsync(message.Sender.Id, cancellationToken);
                // write to db
                await _containerRepository.CreateContainerList(containers.Select(x=> new CreateContainerListRequest
                {
                    Id = x.SourceId,
                    ArticleId = x.ArticleId,
                    CategoryId = x.CategoryId,
                    Quantity = x.Count,
                    Condition = x.ConditionId,
                    Username = x.Username,
                    PriceType = x.PriceWithTax.HasValue ? PriceType.WithTax : PriceType.WithoutTax,
                    Price = x.PriceWithTax.HasValue ? x.PriceWithTax.Value : x.PriceWithoutTax.Value,
                    Currency = x.CurrencyId,
                    Address = x.City,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    UserId = userInfo.Id,
                    MessageId = x.MessageUrl
                }).ToList(), cancellationToken);
                
                await WriteToGoogleSheets(containers, MessengerType.Max);
                
                await _sitePostingService.SendContainersToSite(containers);
                DateTime moscowTime = DateTime.UtcNow.AddHours(3);

                if (moscowTime.Hour >= 9 && moscowTime.Hour < 18)
                {
                    foreach (var container in containers)
                    {
                        var description = DescriptionHelper.GenerateDescription(
                            container.ConditionId,
                            container.CurrencyId,
                            container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                            container.PriceWithoutTax.HasValue
                                ? container.PriceWithoutTax.Value * (decimal)1.1
                                : container.PriceWithTax.Value * (decimal)1.1,
                            container.City,
                            container.CategoryId);
                        await SendMessageToChanel(description,
                            $"https://xn--e1aalcpcdvnp.xn--p1ai/?artnumber={container.ArticleId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _maxBotClient.Messages.SendMessageAsync(
                244266512,
                "Ошибка при записи данных в excel таблицу:" +
                $"{ex.Message}\n" +
                $"username: {message?.Sender?.Name}\n" +
                $"message: {message?.Text}",
                cancellationToken: cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Проверяет наличие обязательных полей в записи контейнера.
    /// Возвращает список отсутствующих полей на русском языке для пользователя.
    /// </summary>
    private List<string> GetMissingFields(AiContainerResponse container)
    {
        var missingFields = new List<string>();

        // Цена - обязательна: хотя бы одна из двух должна быть заполнена
        if ((container.PriceWithTax == null || container.PriceWithTax == 0) &&
            (container.PriceWithoutTax == null || container.PriceWithoutTax == 0))
        {
            missingFields.Add("стоимость контейнера");
        }

        // Size - обязательное поле без дефолтного значения
        if (string.IsNullOrWhiteSpace(container.Size) ||
            container.Size.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("размер контейнера");
        }

        // Type - обязательное поле без дефолтного значения
        if (string.IsNullOrWhiteSpace(container.Type) ||
            container.Type.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("тип контейнера (HC, DC)");
        }

        // City - обязательное поле без дефолтного значения
        if (string.IsNullOrWhiteSpace(container.City) ||
            container.City.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("город");
        }

        return missingFields;
    }

    private async Task<long> SaveOrUpdateUserAsync(
        User user,
        long? chatId,
        CancellationToken cancellationToken,
        bool updateType = false
    )
    {
        try
        {
            var existingUser = await _userRepository.GetByTelegramIdAsync(user.Id, cancellationToken);
            long userId;

            if (existingUser == null)
            {
                var newUser = new CreateUserRequest
                {
                    TelegramId = user.Id,
                    Username = user.Username ?? user.FirstName,
                    UserType = updateType ? UserType.BotUser : UserType.Subscriber,
                    MessengerType = MessengerType.Max
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

            if (existingUser != null && updateType)
            {
                await _userRepository.UpdateUserType(userId, UserType.BotUser);
            }

            return userId;
        }
        catch (Exception ex)
        {
            await _maxBotClient.Messages.SendMessageAsync(
                244266512,
                "возникла ошибка при создании пользователя в Максе:" +
                $"{ex.Message}\n" +
                $"username: {user.Username}, chatid : {chatId}",
                cancellationToken: cancellationToken);
            throw;
        }
    }
    
    private async Task WriteToGoogleSheets(List<ContainerRequestModel> models, MessengerType messengerType)
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
            var currentWeekStart = GetWeekStartMonday(DateTime.UtcNow);
            var sheetName = GetWeekSheetName(currentWeekStart);
            
            await EnsureSheetWithHeadersAsync(sheetsService, spreadsheetId, sheetName);
            
            // Преобразуем модели в данные для Google Sheets
            var values = new List<IList<object>>();

            foreach (var model in models)
            {
                // Безопасное форматирование даты с обработкой null
                string formattedDate = DateTimeOffset.UtcNow.ToString("dd.MM.yyyy HH:mm");

                var row = new List<object>
                {
                    model.ArticleId.ToString(),
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
                    model.Count,
                    messengerType == MessengerType.Max ? "Max" : "Telegram",
                };
                values.Add(row);
            }

            // Динамическое определение диапазона
            var range = $"{sheetName}!A:M";

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
    
    private async Task EnsureSheetWithHeadersAsync(SheetsService service, string spreadsheetId, string sheetName)
    {
        // Получаем список существующих листов
        var getSpreadsheetRequest = service.Spreadsheets.Get(spreadsheetId);
        getSpreadsheetRequest.Fields = "sheets(properties.title)";
        var spreadsheet = await getSpreadsheetRequest.ExecuteAsync();
    
        var sheetExists = spreadsheet.Sheets?.Any(s => 
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true) == true;

        if (!sheetExists)
        {
            Console.WriteLine($"🆕 Создаю новый лист: '{sheetName}'");
        
            // Создаём новый лист
            var batchRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties
                            {
                                Title = sheetName,
                                GridProperties = new GridProperties { RowCount = 5000, ColumnCount = 13 }
                            }
                        }
                    },
                    new Request
                    {
                        SetDataValidation = new SetDataValidationRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = null, // Будет установлено после создания листа
                                StartRowIndex = 1,      // Строка 2 (0-based)
                                EndRowIndex = 5000,     // До строки 5000
                                StartColumnIndex = 1,  // Колонка A
                                EndColumnIndex = 13 // До конца колонки
                            },
                        }
                    }
                }
            };
        
            await service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync();
        
            // Записываем заголовки в первую строку
            var headers = new List<IList<object>>
            {
                new List<object> 
                { 
                   "Артикул", "Размер", "Тип", "Состояние", "Город", "Дата", 
                    "Продавец", "Наличие", "Цена с НДС", "Цена без НДС", 
                    "Валюта", "Количество", "Источник", "Сообщение"
                }
            };
        
            var headerRange = new ValueRange { Values = headers };
            var headerRequest = service.Spreadsheets.Values.Update(headerRange, spreadsheetId, $"{sheetName}!A1:M1");
            headerRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await headerRequest.ExecuteAsync();
        }
    }
    
    private static DateTime GetWeekStartMonday(DateTime date)
    {
        // Вычисляем, сколько дней нужно отнять, чтобы попасть в понедельник
        int daysToMonday = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-daysToMonday).Date;
    }

    // 🏷️ Формирование имени листа: "24.03.25-30.03.25"
    // Формат безопасен для Google Sheets (без запрещённых символов \ / ? * [ ])
    private static string GetWeekSheetName(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return $"{weekStart:dd.MM.yy}-{weekEnd:dd.MM.yy}";
    }

    public async Task SendMessageToChanel(string text, string url)
    {
        await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest()
        {
            Text = $"{text}\nСсылка: {url}\nНомер телефона: +7(931)521-07-67"
        }, -72880335247520);
    }
}