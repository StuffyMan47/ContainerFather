using System.Text;
using System.Text.Json;
using ContainerFather.Bot.AiTunnelService;
using ContainerFather.Core.Interfaces.Settings.Models;
using ContainerFather.Infrastructure.Clients.Dto;
using Microsoft.Extensions.Options;

namespace ContainerFather.Infrastructure.Clients;

public class AiTunnelClient : IAiTunnelClient
{
  private readonly IOptions<BotConfiguration> _options;
  
    public AiTunnelClient(IOptions<BotConfiguration> options)
    {
      _options = options;
    }
    
    public async Task<string> SendMessage(string message)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_options.Value.AiUri);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Value.AiToken}");
        client.DefaultRequestHeaders.Add("User-Agent", "CSharpClient/1.0");

        // Формирование запроса
        var request = new
        {
            model = "deepseek/deepseek-v3.2",
            messages = new[]
            {
                new { role = "user", content = $"{Prompt1} Вот само сообщение: {message}" }
            },
            max_tokens = 50000
        };

        // Отправка запроса
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = await client.PostAsync("chat/completions", 
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Обработка ответа
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();

        // Десериализация ответа
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return completion.Choices[0].Message.Content;
    }

    public const string Prompt1 = """
                                  Твоя задача  
                                  Ты — алгоритм обработки текстовых сообщений о продаже/покупке морских контейнеров.  
                                  Конвертируй входной текст в JSON-массив объектов**, строго соответствующих C#-модели ниже.  
                                  Используй только данные из текста, применяй правила ниже для нормализации. Если данные отсутствуют — используй значения по умолчанию (указаны в правилах).  
                                  public class AiContainerResponse
                                  {
                                      public required string Size { get; set; } // Обязательное: "20", "40" и т.д.
                                      public required string Type { get; set; } // Обязательное: "HC", "DC", "NEW", "CW"
                                      public string? Condition { get; set; } // Опционально: "Б/У", "Новый"
                                      public required string City { get; set; } // Обязательное: название города (исправить опечатки!)
                                      public required string Availability { get; set; } // Обязательное: "В наличии", "По запросу"
                                      
                                      // Обязательно один из видов цен должен быть заполнен
                                      public decimal? PriceWithTax { get; set; } // Опционально: число в рублях (умножить "тыс" на 1000)
                                      public decimal? PriceWithoutTax { get; set; } // Опционально
                                      
                                      public required string Currency { get; set; } // Обязательное: "RUB" (по умолчанию), "USD"
                                      public required string TransactionType { get; set; } // Обязательное: "Продажа" или "Покупка"
                                  }
                                  Правила обработки  
                                  1. Разделение на записи  
                                  - Каждая отдельная позиция** в тексте = один объект в JSON.  
                                    - Пример:
                                      ```
                                      Москва 20DC - 100 тыс  
                                      СПб 40HC NEW - 200 тыс  
                                      ```  
                                      2 записи в массиве.  
                                  - Если в сообщении есть #продам** и #куплю** — разделить на разные записи с разными `TransactionType`.  
                                  - Игнорируй служебные фразы («ВСЕМ ПРИВЕТ!», «Коллеги, добрый день», «Предложение действует до...»).

                                  2. Поля с жесткими правилами**  
                                  - `TransactionType`  
                                    - `#продам`, «Продам», «Предлагаем» → `"Продажа"`  
                                    - `#куплю`, «Куплю» → `"Покупка"`  
                                    - Если оба типа в тексте — **разделить на две записи**.  

                                  - `Currency`  
                                    - По умолчанию: `"RUB"`  
                                    - Только если явно указано: `$`, «доллар» → `"USD"`  

                                  3. Нормализация данных**  
                                  - `Size` и `Type`  
                                    - `Size`: извлеки цифры перед «фут», «HC», «DC» (например, «40HC» → `"40"`, «20 фут» → `"20"`).  
                                    - `Type`:  
                                      - «HC», «HC б/у» → `"HC"`  
                                      - «DC», «20 фут» → `"DC"`  
                                      - «NEW», «CW» → `"NEW"` / `"CW"`  
                                      - «НС», «HС» → исправить на `"HC"` (опечатки).  

                                  - `City`  
                                    - Исправь опечатки:  
                                      - «Новосибисрк» → `"Новосибирск"`  
                                      - «МСК» → `"Москва"`  
                                      - «СПб» → `"Санкт-Петербург"`  
                                      - «Екб» → `"Екатеринбург"`  
                                    - Если город не указан явно (например, «1*40НС МСК - Владивосток») — брать **первый город** («Москва»).  

                                  - `PriceWithTax` / `PriceWithoutTax`  
                                    - Правило 1:** Если есть «с НДС» → заполни `PriceWithTax` (число × 1000, если есть «тыс»).  
                                    - Правило 2:** Если есть «без НДС» → заполни `PriceWithoutTax`.  
                                    - Правило 3:** Если два числа в скобках:  
                                      ```
                                      68 000 c НДС (82 900 без НДС)
                                      ```  
                                      → `PriceWithTax: 68000`, `PriceWithoutTax: 82900`  
                                    - Правило 4:** «от 100 тыс» → `PriceWithTax: 100000`, `Availability: "По запросу"`.  
                                    - Правило 5:** Если цена указана без НДС/без пометок → `PriceWithTax: null`, `PriceWithoutTax: [число]`.  

                                  - `Condition`  
                                    - «б/у», «used» → `"Б/У"`  
                                    - «NEW», «новый» → `"NEW"`  
                                    - «CW» → `"CW"`  

                                  - `Availability`  
                                    - «много», «В наличии» → `"В наличии"`  
                                    - «от 100 тыс», «по запросу» → `"По запросу"`  
                                    
                                  Формат вывода  
                                  - Только валидный JSON-массив объектов `ContainerRequestModel`.  
                                  - Никакого дополнительного текста до/после JSON.  
                                  - Все обязательные поля должны быть заполнены (используй значения по умолчанию, если данных нет).  

                                  Примеры для обучения**  
                                  Пример 1:  
                                  Вход:  
                                  ```
                                  #Продам 
                                  Благовещенск 40HC - 100 тыс
                                  Омск 40 фут от 130 тыс с ндс 
                                  ```  
                                  Выход:  
                                  [
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "Condition": null,
                                      "City": "Благовещенск",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": 100000,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Продажа"
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "DC",
                                      "Condition": null,
                                      "City": "Омск",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": "По запросу",
                                      "PriceWithTax": 130000,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Продажа"
                                    }
                                  ]

                                  Пример 2:  
                                  Вход:  
                                  ```text
                                  👉Екатеринбург 20dc б/у -  68 000 c НДС (82 900 без НДС)
                                  👉СПб 40 НС NEW-  249 000 с НДС (260 000 c НДС)
                                  ```  
                                  Выход:  
                                  [
                                    {
                                      "Size": "20",
                                      "Type": "DC",
                                      "Condition": "Б/У",
                                      "City": "Екатеринбург",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": 68000,
                                      "PriceWithoutTax": 82900,
                                      "Currency": "RUB",
                                      "TransactionType": "Продажа"
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "Condition": "NEW",
                                      "City": "Санкт-Петербург",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": 249000,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Продажа"
                                    }
                                  ]

                                  Пример 3 (сложный):  
                                  Вход:  
                                  ```text
                                  #продам 
                                  Продам 40HC CW в Екатеринбурге 105тр 

                                  #куплю 
                                  Куплю 40HC CW в Самаре, Тольятти 
                                  ```  
                                  Выход:  
                                  [
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "Condition": "CW",
                                      "City": "Екатеринбург",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": 105000,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Продажа"
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "Condition": "CW",
                                      "City": "Самара",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": null,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Покупка"
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "Condition": "CW",
                                      "City": "Тольятти",
                                      "Date": "2026-01-09T00:00:00+03:00",
                                      "Username": "Unknown",
                                      "Availability": null,
                                      "PriceWithTax": null,
                                      "PriceWithoutTax": null,
                                      "Currency": "RUB",
                                      "TransactionType": "Покупка"
                                    }
                                  ]

                                  Критически важные замечания  
                                  1. Никаких предположений вне правил!  
                                  2. Опечатки — твоя ответственность. Города и типы контейнеров должны быть нормализованы.  
                                  3. Цены в «тыс» умножай на 1000 («100 тыс» → `100000`).  
                                  4. Если в тексте нет данных для обязательного поля — используй значения по умолчанию из правил. А если значения по умолчанию нет, то значение должно быть null.

                                  Начинай обработку только после получения текста. Выводи ТОЛЬКО JSON.
                                  Если ты понимаешь, что тут нет информации о контейнерах, то возвращай только слово "Амбасадор" и ничего больше
                                  """;
}