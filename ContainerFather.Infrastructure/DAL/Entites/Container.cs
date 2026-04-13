using System.Text.Json.Serialization;
using ContainerFather.Core.Enums.SiteEnums;

namespace ContainerFather.Infrastructure.DAL.Entites;

public class Container
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
    public DateTimeOffset CreatedAt { get; init; } =  DateTimeOffset.UtcNow;
    public long? UserId { get; set; }
    public long? MessageId { get; set; }

    public List<User> Users { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}