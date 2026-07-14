using ContainerFather.Core.Entities;
using ContainerFather.Core.Enums;

namespace ContainerFather.Infrastructure.DAL.Entites;

public class Chat : BaseEntity
{
    public required string Name { get; set; }
    public required long ChatId { get; set; } 
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; init; } =  DateTimeOffset.UtcNow;
    public required MessengerType MessengerType { get; init; }
    
    public List<Message> Messages { get; set; } = [];
    public List<User> Users { get; set; } = [];
}