using System.Text.Json.Serialization;

namespace ContainerFather.Bot.SiteService.Model;

public class SendContainersInfoResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; }
    
    [JsonPropertyName("created")]
    public List<string> Created { get; set; } = [];
    
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];
}