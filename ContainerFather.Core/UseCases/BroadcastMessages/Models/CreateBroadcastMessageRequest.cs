using ContainerFather.Core.Enums;

namespace ContainerFather.Core.UseCases.BroadcastMessages.Models;

public class CreateBroadcastMessageRequest
{
    public required string Message { get; set; }
    public BroadcastMessagePeriodType PeriodType { get; set; }
}