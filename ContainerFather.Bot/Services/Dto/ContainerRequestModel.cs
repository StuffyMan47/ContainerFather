using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Bot.Services.Dto;

public class ContainerRequestModel
{
    public CategoryEnum CategoryId { get; set; }
    
    public required Guid SourceId { get; set; }
    
    public long ArticleId { get; set; }
    
    /// <summary>
    /// Размер
    /// </summary>
    public required string Size { get; set; }
    
    /// <summary>
    /// Тип
    /// </summary>
    public required string Type { get; set; }
    
    /// <summary>
    /// Состояние
    /// </summary>
    public string? Condition { get; set; }
    
    public ConditionEnum ConditionId { get; set; }
    
    /// <summary>
    /// Город
    /// </summary>
    public required string City { get; set; }
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    /// <summary>
    /// Дата
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Продавец
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Наличие
    /// </summary>
    public string? Availability {get; set;}
    
    /// <summary>
    /// Цена с НДС
    /// </summary>
    public decimal? PriceWithTax { get; set; }
    
    /// <summary>
    /// Цена без НДС
    /// </summary>
    public decimal? PriceWithoutTax { get; set; }
    
    /// <summary>
    /// Валюта
    /// </summary>
    public required string Currency { get; set; }
    
    public required CurrencyEnum CurrencyId { get; set; }
    
    /// <summary>
    /// Количество
    /// </summary>
    public required int Count { get; set; }
    
    /// <summary>
    /// Будет ли опубликована позиция на сайт
    /// </summary>
    public bool IsPublicToSite { get; set; }
}