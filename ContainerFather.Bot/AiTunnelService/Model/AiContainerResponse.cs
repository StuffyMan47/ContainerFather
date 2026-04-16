using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Bot.AiTunnelService.Model;

public class AiContainerResponse
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
    public string? ConditionName { get; set; }
    
    public ConditionEnum? ConditionId { get; set; }
    
    /// <summary>
    /// Город
    /// </summary>
    public required string City { get; set; }
    
    public string? FullAddress { get; set; }

    /// <summary>
    /// Наличие
    /// </summary>
    public required string Availability {get; set;}
    
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
    public required CurrencyEnum Currency { get; set; }
    
    /// <summary>
    /// Количество
    /// </summary>
    public required int Count { get; set; }
    
    /// <summary>
    /// Определенная категория для сайта
    /// </summary>
    public CategoryEnum CategoryId { get; set; }
}