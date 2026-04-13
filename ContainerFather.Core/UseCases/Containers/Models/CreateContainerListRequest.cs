using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Core.UseCases.Containers.Models;

public class CreateContainerListRequest
{
    public Guid Id { get; set; }
    public CategoryEnum CategoryId { get; set; }
    public required int Quantity { get; set; }
    public ConditionEnum Condition { get; set; }
    public required string Username { get; set; }
    public PriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public CurrencyEnum Currency { get; set; }
    public string Address { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public long? UserId { get; set; }
    public long? MessageId { get; set; }
}