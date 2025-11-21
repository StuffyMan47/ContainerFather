namespace ContainerFather.Core.UseCases.Users.Models;

public class GetUserStatisticResponse
{
    public required string Username { get; set; }
    public int MessageCount { get; set; }

    public List<ChatStatisticResponse> Statistics { get; set; } = [];
}

public class ChatStatisticResponse
{
    public required string ChatName { get; set; }
    public int MessageCount { get; set; }
    public List<string> Messages { get; set; } = [];
}