namespace ContainerFather.Bot.Services.Dto;

public class ContainerRequestModel
{
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
    
    /// <summary>
    /// Город
    /// </summary>
    public required string City { get; set; }
    
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
    
    /// <summary>
    /// Тип сделки
    /// </summary>
    public required string TransactionType { get; set; }
}