using System.Text.Json.Serialization;
using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Bot.SiteService.Model;

public class SendContainersInfoRequest
{
    /// <summary>
    /// Артикул
    /// </summary>
    [JsonPropertyName("sourceId")]
    public Guid SourceId { get; set; }
    
    /// <summary>
    /// Содержит инфу о типе и размере контейнера
    /// </summary>
    [JsonPropertyName("categoryId")]
    public CategoryEnum CategoryId { get; set; }

    /// <summary>
    /// Количество
    /// </summary>
    [JsonPropertyName("quantity")]
    public required int Quantity { get; set; }
    
    /// <summary>
    /// Состояние
    /// </summary>
    [JsonPropertyName("condition")]
    public ConditionEnum Condition { get; set; }

    /// <summary>
    /// Город
    /// </summary>
    [JsonPropertyName("city")]
    public required string City { get; set; }
    
    /// <summary>
    /// Местоположения в широте и долготе
    /// </summary>
    [JsonPropertyName("location")]
    public required LocationDetails Location { get; set; }
    
    /// <summary>
    /// username в telegram
    /// </summary>
    [JsonPropertyName("telegramUsername")]
    public required string Username { get; set; }
    
    /// <summary>
    /// Номер телефона, если есть
    /// </summary>
    [JsonPropertyName("phone")]
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Тип цены: С ндс или без
    /// </summary>
    [JsonPropertyName("priceType")]
    public PriceType PriceType { get; set; }
    
    /// <summary>
    /// Цена
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Валюта
    /// </summary>
    [JsonPropertyName("currency")]
    public required CurrencyEnum Currency { get; set; }
    
    /// <summary>
    /// Полный адрес
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class LocationDetails
{
    [JsonPropertyName("latitude")]
    public float Latitude { get; set; }
    [JsonPropertyName("longitude")]
    public float Longitude { get; set; }
}