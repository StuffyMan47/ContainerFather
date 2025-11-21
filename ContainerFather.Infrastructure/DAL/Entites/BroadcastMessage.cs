using ContainerFather.Core.Entities;
using ContainerFather.Core.Enums;

namespace ContainerFather.Infrastructure.DAL.Entites;

public class BroadcastMessage : BaseEntity
{
    public required string Message { get; set; }
    public bool IsActive { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public BroadcastMessagePeriodType PeriodType { get; set; }
}