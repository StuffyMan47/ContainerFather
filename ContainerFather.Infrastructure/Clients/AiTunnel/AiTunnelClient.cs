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
        client.Timeout = TimeSpan.FromSeconds(600);
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
            max_tokens = 10000,
            chat_template_kwargs = new
            {
              enable_thinking = false,
            }
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
                                  Вот нужные для определения значения перечисления: 
                                  Валюта (CurrencyEnum)
                                  7 - рубль
                                  8 - доллар
                                  9 - евро
                                  10 - тенге
                                  11 - гривна
                                  12 - белорусский рубль
                                  
                                  Состояние (ConditionEnum)
                                  5 - новый
                                  6 - б/у
                                  
                                  Категории (CategoryEnum)
                                  5 - Морские и ЖД контейнеры
                                  -- 6 - Контейнеры 10 футов
                                  -- 7 - Контейнеры 20 футов
                                  -- 8 - Контейнеры 40 футов
                                  -- 9 - Контейнеры 45 футов
                                  -- 24 - Контейнеры 5 тонн
                                  -- 25 - контейнеры 3 тонны
                                  53 - High Cube контейнеры
                                  -- 54 - High Cube контейнеры 20 футов
                                  -- 55 - High Cube контейнеры 40 футов
                                  17 - Open Top контейнеры
                                  -- 18 - Open Top контейнеры 20 футов
                                  -- 19 - Open Top контейнеры 40 футов
                                  20 - Flat Rack контейнеры
                                  -- 21 - Flat Rack контейнеры 20 футов
                                  -- 22 - Flat Rack контейнеры 40 футов
                                  10 - Рефрижераторные контейнеры
                                  -- 13 - Рефконтейнеры 20 футов
                                  -- 12 - Рефконтейнеры 40 футов
                                  -- 11 - Рефконтейнеры 45 футов
                                  14 - Танк-контейнеры
                                  -- 16 - Танк-контейнеры 20 футов
                                  -- 15 - Танк-контейнеры 40 футов
                                  26 - Бытовки
                                  -- 32 - Деревянные бытовки
                                  -- 33 - Металлические бытовки
                                  -- 28 - Дачные бытовки
                                  -- 27 - Строительные бытовки
                                  -- 56 - Утепленные бытовки
                                  -- 29 - Бытовки-вагончики
                                  -- 30 - Бытовки-бани
                                  31 - Модульные здания
                                  34 - Блок-контейнеры
                                  35 - Мусорные контейнеры
                                  -- 36 - Мультилифт-контейнеры
                                  -- 37 - Бункер-контейнеры
                                  -- 39 - до 1.3 м3
                                  -- 57 - от 8 м3
                                  -- 40 - Урны до 0.5 м3
                                  -- 41 - Пресс-контейнеры
                                  42 - Запчасти для контейнеров
                                  43 - Услуги
                                  -- 44 - Перевозка
                                  -- 45 - Аренда
                                  -- 46 - Ремонт
                                  -- 47 - Хранение
                                  -- 48 - Страхование
                                  49 - Новости, События, Выставки
                                  50 - Терминалы, площадки, склады
                                  51 - Прочее
                                  52 - Рекламные объявления
                                  public class AiContainerResponse
                                  {
                                      public required string Size { get; set; } // Обязательное: "20", "40" и т.д.
                                      public required string Type { get; set; } // Обязательное: "DC", "HC", "OT", "FR", "TK",
                                      public required CategoryEnum CategoryId { get; set; } // Обязательное: значения взять из enum CategoryEnum
                                      public string? ConditionName { get; set; } // Обязательное: "Б/У", "Новый"
                                      public ConditionEnum? ConditionId { get; set; } // Обязательное: 5 - Новый, 6 - Б/У.
                                      public required string City { get; set; } // Обязательное: название города (исправить опечатки!)
                                      public string? FullAddress { get; set; } // Опционально: Полный адрес
                                      public required string Availability { get; set; } // Обязательное: "В наличии", "По запросу"
                                      
                                      // Обязательно один из видов цен должен быть заполнен
                                      public decimal? PriceWithTax { get; set; } // Опционально: число в рублях (умножить "тыс" на 1000)
                                      public decimal? PriceWithoutTax { get; set; } // Опционально
                                      public required int Count { get; set; }
                                      public required CurrencyEnum Currency { get; set; } // Обязательное: 7 (по умолчанию), значения брать из enum CurrencyEnum
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
                                  - Если в сообщении есть #выдам или #куплю, без упоминания продажи, то возвращай только слово "Амбасадор" и ничего больше
                                  - Если в сообщении есть #продам**, то нужно брать позиции только из этого блока и игнорировать блоки #куплю** или #выдам** 
                                  - Игнорируй служебные фразы («ВСЕМ ПРИВЕТ!», «Коллеги, добрый день», «Предложение действует до...»).

                                  2. Нормализация данных**  
                                  - `Size` и `Type`  
                                    - `Size`: извлеки цифры перед «фут», «HC», «DC» (например, «40HC» → `"40"`, «20 фут» → `"20"`).  
                                    - `Type`:  
                                      - «HC», «HC б/у» → `"HC"`  
                                      - «DC», «20 фут» → `"DC"`  
                                      - «НС», «HС» → исправить на `"HC"` (опечатки).  

                                  - `City`  
                                    - Исправь опечатки:  
                                      - «Новосибисрк» → `"Новосибирск"`  
                                      - «МСК» → `"Москва"`  
                                      - «СПб» → `"Санкт-Петербург"`  
                                      - «Екб» → `"Екатеринбург"`  
                                    - Если город не указан явно (например, «1*40НС МСК - Владивосток») — брать **первый город** («Москва»). 
                                    
                                  - `FullAddress`
                                    - Если помимо города указан полный адрес, то целиком записывай его в это поле 

                                  - `PriceWithTax` / `PriceWithoutTax`  
                                    - Правило 1:** Если есть «с НДС» → заполни `PriceWithTax` (число × 1000, если есть «тыс»).  
                                    - Правило 2:** Если есть «без НДС» → заполни `PriceWithoutTax`.  
                                    - Правило 3:** Если два числа в скобках:  
                                      ```
                                      68 000 c НДС (82 900 без НДС)
                                      ```  
                                      → `PriceWithTax: 68000`, `PriceWithoutTax: 82900`  
                                    - Правило 4:** «от 100 тыс» → `PriceWithTax: 100000`.  
                                    - Правило 5:** Если цена указана без НДС/без пометок → `PriceWithoutTax: [число]`, `PriceWithTax: null`.  

                                  - `Availability`  
                                    - «много», «В наличии» → `"В наличии"`  
                                    - «от 100 тыс», «по запросу» → `"По запросу"`  
                                    
                                  - `Count`
                                    - Если не указано количество → заполни `1`. 
                                    
                                  Формат вывода  
                                  - Только валидный JSON-массив объектов `AiContainerResponse`. Без markdown-блоков (```json), без пояснений, без преамбулы.
                                  - Никакого дополнительного текста до/после JSON.  
                                  - Все обязательные поля должны быть заполнены (используй значения по умолчанию, если данных нет).  

                                  Примеры для обучения**  
                                  Пример 1:  
                                  Вход:  
                                  ```
                                  #продам 
                                  Специальные цены с Trans Russia 2026
                                  действуют до 11.04.2026:
                                  
                                  Екатеринбург
                                  20DC 6/y-79 000 Р с НДС
                                  
                                  Москва
                                  40HC 6/y-89 500 Р с НДС
                                  ```  
                                  Выход:  
                                  [
                                    {
                                      "Size": "20",
                                      "Type": "DC",
                                      "ConditionName": "Б/У",
                                      "ConditionId": 6,
                                      "City": "Екатеринбург",
                                      "Availability": "В наличии",
                                      "PriceWithTax": 79000,
                                      "PriceWithoutTax": null,
                                      "Currency": 7,
                                      "Count": 1,
                                      "CategoryId": 7
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "ConditionName": "Б/У",
                                      "ConditionId": 6,
                                      "City": "Москва",
                                      "Availability": "В наличии",
                                      "PriceWithTax": 89500,
                                      "PriceWithoutTax": null,
                                      "Currency": 7,
                                      "Count": 1,
                                      "CategoryId": 55
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
                                      "ConditionName": "Б/У",
                                      "ConditionId": 6,
                                      "City": "Екатеринбург",
                                      "Availability": "В наличии",
                                      "PriceWithTax": 68000,
                                      "PriceWithoutTax": 82900,
                                      "Currency": 7,
                                      "Count": 1,
                                      "CategoryId": 7
                                    },
                                    {
                                      "Size": "40",
                                      "Type": "HC",
                                      "ConditionName": "Новый",
                                      "ConditionId": 5,
                                      "City": "Санкт-Петербург",
                                      "Availability": "В наличии",
                                      "PriceWithTax": 249000,
                                      "PriceWithoutTax": 260000,
                                      "Currency": 7,
                                      "Count": 1,
                                      "CategoryId": 55
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
                                      "ConditionName": "Б/У",
                                      "ConditionId": 6,
                                      "City": "Екатеринбург",
                                      "Availability": "В наличии",
                                      "PriceWithTax": 105000,
                                      "PriceWithoutTax": null,
                                      "Currency": 7,
                                      "Count": 1,
                                      "CategoryId": 55
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