namespace ContainerFather.Core.Entities;

public class BaseEntity
{
    public long Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; } =  DateTimeOffset.UtcNow;
}