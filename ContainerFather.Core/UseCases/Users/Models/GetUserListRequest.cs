using ContainerFather.Core.Enums;

namespace ContainerFather.Core.UseCases.Users.Models;

public record GetUserListRequest
{
    public bool OnlyActive { get; set; }
    public UserType? UserType { get; set; } = null;
}