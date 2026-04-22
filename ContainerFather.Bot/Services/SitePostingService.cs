using System.Globalization;
using ContainerFather.Bot.AiTunnelService;
using ContainerFather.Bot.Helpers;
using ContainerFather.Bot.Services.Dto;
using ContainerFather.Bot.Services.Interfaces;
using ContainerFather.Bot.SiteService;
using ContainerFather.Bot.SiteService.Model;
using ContainerFather.Core.Enums.SiteEnums;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Core.UseCases.BroadcastMessages.Interfaces;
using ContainerFather.Core.UseCases.Chats.Interfaces;
using ContainerFather.Core.UseCases.Containers.Interfaces;
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace ContainerFather.Bot.Services;

public class SitePostingService : ISitePostingService
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
    private readonly ISiteClient _siteClient;
    private readonly IContainerRepository _containerRepository;
    
    public SitePostingService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IBroadcastMessageRepository broadcastMessageRepository,
        IChatRepository chatRepository,
        IAdminDialogService adminDialogService,
        IGetStatisticHandler getStatisticHandler,
        IBroadcastService broadcastService,
        ISiteClient siteClient,
        IContainerRepository containerRepository,
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
        _siteClient = siteClient ?? throw new ArgumentNullException(nameof(siteClient));
        _containerRepository = containerRepository ?? throw new ArgumentNullException(nameof(containerRepository));
    }
    
    public async Task SendConfirmToAdmin()
    {
        var adminId = "1037799385"; // Указан фиксированный ID админа, как запрошено
        var currentDate = DateTimeOffset.UtcNow.ToString("dd.MM.yyyy HH:mm");
        var messageText = $"Пора отметить чекбоксы в гугл таблице. Текущее время: {currentDate}";
        
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "Отправить выбранные предложения на сайт",
                    callbackData: $"post_to_site_{currentDate}")
            }
        });

        await _botClient.SendMessage(
            chatId: adminId,
            text: messageText,
            replyMarkup: inlineKeyboard);
    }

    public async Task ReadGoogleTable(DateTimeOffset date)
    {
        var credential = GoogleCredential.FromJson(_botConfiguration.GoogleAuth.Key)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        var sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ContainerFather.Bot",
        });

        var spreadsheetId = "1Q4aHnNPNFXxlwTxRNJk9IUf1m6V2wWV1HTc3rnu-ZbE";

        try
        {
            // Получаем список листов
            var spreadsheet = await sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            if (spreadsheet.Sheets == null || spreadsheet.Sheets.Count == 0)
            {
                return;
            }

            // Берем последний лист
            var lastSheetTitle = spreadsheet.Sheets.Last().Properties.Title;
            var range = $"{lastSheetTitle}!A:M"; // Запрашиваем данные с A по M колонки

            var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            if (response.Values == null || response.Values.Count == 0)
            {
                return;
            }

            var selectedIds = new List<Guid>();

            // Пропускаем заголовок (если есть) или начинаем с первой строки
            foreach (var row in response.Values.Skip(1))
            {
                // Проверяем, что в строке достаточно колонок (до L включительно, индекс 11)
                if (row.Count > 11)
                {
                    var idStr = row[0]?.ToString(); // Колонка A
                    var dateStr = row[5]?.ToString(); // Колонка C
                    var isCheckedStr = row[12]?.ToString(); // Колонка L

                    if (Guid.TryParse(idStr, out var id) &&
                        !string.IsNullOrEmpty(dateStr) &&
                        DateTime.TryParseExact(dateStr, "dd.MM.yyyy HH:mm", 
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowDate) &&
                        rowDate.Date == date.Date &&  // 👈 Сравнение только по дате (без времени)
                        (isCheckedStr?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        selectedIds.Add(id);
                    }
                }
            }

            if (selectedIds.Count > 0)
            {
                var containerResponses = await _containerRepository.GetContainerList(selectedIds, null, CancellationToken.None);
                
                var containersToPost = containerResponses.Select(c => new ContainerRequestModel
                {
                    SourceId = c.Id,
                    ConditionId = c.Condition,
                    City = c.Address,
                    CurrencyId = c.Currency,
                    Currency = c.Currency.ToString(),
                    Count = c.Quantity,
                    PriceWithoutTax = c.PriceType == PriceType.WithoutTax ? c.Price : null,
                    PriceWithTax = c.PriceType == PriceType.WithTax ? c.Price : null,
                    Username = c.Username,
                    CategoryId = c.CategoryId,
                    Size = c.CategoryId.ToString(), // Adding placeholder, real data might require separate parsing
                    Type = c.CategoryId.ToString() // Adding placeholder, real data might require separate parsing
                }).ToList();

                await SendContainersToSite(containersToPost);
            }
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                "714862316",
                "Не получилось прочитать Google таблицу: " +
                $"{ex.Message}");
        }
    }

    public async Task SendContainersToSite(List<ContainerRequestModel> containers)
    {
        try
        {
            List<SendContainersInfoRequest> requests = [];
            foreach (var container in containers)
            {
                var city = CityGeoService.GetCityCoordinatesAsync(container.City);
                var description = DescriptionHelper.GenerateDescription(
                    container.ConditionId, 
                    container.CurrencyId,
                    container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                    container.PriceWithoutTax.HasValue ? container.PriceWithoutTax.Value * (decimal)1.1 : container.PriceWithTax.Value * (decimal)1.1,
                    container.City,
                    container.CategoryId);
                
                var request = new SendContainersInfoRequest
                {
                    SourceId = container.SourceId,
                    Condition = container.ConditionId,
                    Address = container.City,
                    Currency = container.CurrencyId,
                    Quantity = container.Count,
                    PhoneNumber = null,
                    PriceType = container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                    Price = container.PriceWithoutTax.HasValue ? container.PriceWithoutTax.Value * (decimal)1.1 : container.PriceWithTax.Value * (decimal)1.1,
                    Location = new LocationDetails()
                    {
                        Latitude = city.Latitude.Value,
                        Longitude = city.Longitude.Value,
                    },
                    Username = container.Username,
                    CategoryId = container.CategoryId,
                    Description = description,
                };
                requests.Add(request);
            }
            var result = await _siteClient.SendContainersInfo(requests, CancellationToken.None);
            
            await _botClient.SendMessage(
                "1037799385",
                $"Результат отправки объявлений на сайт {result.Result}\n" +
                $"Выложены записи с артикулами {string.Join(", ", result.Created)}\n" +
                $"Ошибки: {string.Join(", ", result.Errors)}");
            
            await _botClient.SendMessage(
                "714862316",
                $"Результат отправки объявлений на сайт {result.Result}\n" +
                $"Выложены записи с артикулами {string.Join(", ", result.Created)}\n" +
                $"Ошибки: {string.Join(", ", result.Errors)}");
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                "714862316",
                "Не получилось отправить контейнеры на сайт \n" +
                $"{ex.Message}");
            await _botClient.SendMessage(
                "1037799385",
                "Не получилось отправить контейнеры на сайт \n" +
                $"{ex.Message}");
        }
    }
}