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
using ContainerFather.Core.UseCases.Messages.Interfaces;
using ContainerFather.Core.UseCases.Users.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;

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
    
    public SitePostingService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IBroadcastMessageRepository broadcastMessageRepository,
        IChatRepository chatRepository,
        IAdminDialogService adminDialogService,
        IGetStatisticHandler getStatisticHandler,
        IBroadcastService broadcastService,
        ISiteClient siteClient,
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
    }
    
    public async Task SendContainersToSite(List<ContainerRequestModel> containers)
    {
        try
        {
            List<SendContainersInfoRequest> requests = [];
            foreach (var container in containers)
            {
                var city = CityGeoService.GetCityCoordinatesAsync(container.City);
                var request = new SendContainersInfoRequest
                {
                    SourceId = container.SourceId,
                    Condition = container.ConditionId,
                    Address = container.City,
                    Currency = container.CurrencyId,
                    Quantity = container.Count,
                    PhoneNumber = null,
                    PriceType = container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                    Price = container.PriceWithoutTax.HasValue ? container.PriceWithoutTax.Value : container.PriceWithTax.Value,
                    Location = new LocationDetails()
                    {
                        Latitude = city.Latitude.Value,
                        Longitude = city.Longitude.Value,
                    },
                    Username = container.Username,
                    CategoryId = container.CategoryId,
                    
                };
                requests.Add(request);
            }
            await _siteClient.SendContainersInfo(requests, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(
                "714862316",
                "Не получилось отправить контейнеры на сайт " +
                $"{ex.Message}");
        }
    }
}