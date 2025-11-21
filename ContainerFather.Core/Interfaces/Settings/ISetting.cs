using ContainerFather.Core.Interfaces.Settings.Models;

namespace ContainerFather.Core.Interfaces.Settings;

public interface ISetting
{
    public BotConfiguration BotConfiguration { get; set; }
}