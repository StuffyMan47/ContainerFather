using System.Text;
using System.Text.Json;
using ContainerFather.Bot.SiteService;
using ContainerFather.Bot.SiteService.Model;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Infrastructure.Clients.Dto;
using ContainerFather.Infrastructure.Clients.Site.Dto;
using Microsoft.Extensions.Options;

namespace ContainerFather.Infrastructure.Clients.Site;

public class SiteClient : ISiteClient
{
    private readonly IOptions<BotConfiguration> _options;
  
    public SiteClient(IOptions<BotConfiguration> options)
    {
        _options = options;
    }

    public async Task SendContainersInfo(List<SendContainersInfoRequest> request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_options.Value.SiteUrl);
        client.DefaultRequestHeaders.Add("X-API-KEY", _options.Value.SiteToken);
        client.DefaultRequestHeaders.Add("User-Agent", "CSharpClient/1.0");

        // Отправка запроса
        var json = JsonSerializer.Serialize(request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = await client.PostAsync("containers/add-list",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Обработка ответа
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();

        // Десериализация ответа
        var completion = JsonSerializer.Deserialize<SendContainersInfoResponse>(responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}