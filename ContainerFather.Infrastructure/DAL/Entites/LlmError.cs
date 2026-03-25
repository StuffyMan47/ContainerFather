using ContainerFather.Bot.AiTunnelService.Model;
using ContainerFather.Core.Entities;

namespace ContainerFather.Infrastructure.DAL.Entites;

public class LlmError : BaseEntity
{
    public required string TelegramMessage { get; set; }
    public required string Prompt { get; set; }
    public required string LlmRequest { get; set; }
    public required string LlmResponse { get; set; }
    public required string ErrorMessage { get; set; }
    public AiContainerResponse? ContainerResponse { get; set; }
}