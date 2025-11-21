namespace ContainerFather.Core.UseCases.Users.Models;

public record GetUserListRequest
{
    public bool OnlyActive { get; set; }
}