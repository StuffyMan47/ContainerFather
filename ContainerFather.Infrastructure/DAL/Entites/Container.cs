using System.Text.Json.Serialization;
using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Infrastructure.DAL.Entites;

public class Container
{
    public Guid Id { get; set; }
    
    // Unique ID for human-readable reference generated in code
    public long ArticleId { get; set; }
    
    public CategoryEnum CategoryId { get; set; }
    public required int Quantity { get; set; }
    public ConditionEnum Condition { get; set; }
    public required string Username { get; set; }
    public PriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public CurrencyEnum Currency { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset CreatedAt { get; init; } =  DateTimeOffset.UtcNow;
    public long? UserId { get; set; }
    public string? MessageId { get; set; }

    public User User { get; set; }
}