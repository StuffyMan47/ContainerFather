namespace ContainerFather.Core.UseCases.Chats.Models;

public class GetChatStatisticResponse
{
    public required string ChatName { get; set; }
    public int MessageCount { get; set; }
    public List<PersonalStatistic> PersonalStatistics { get; set; } = [];
}

public class PersonalStatistic
{
    public long UserId { get; set; }
    public required string UserName { get; set; }
    public int MessageCount { get; set; }
}