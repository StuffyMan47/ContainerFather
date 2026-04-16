using ContainerFather.Bot.Services.Interfaces;
using Hangfire;

namespace ContainerFather.Bot.BackgroundJobs.Jobs;

[Queue("default")]
[AutomaticRetry(Attempts = 0)]
public class SendConfirmToSitePostJob
{
    private readonly ISitePostingService _sitePostingService;

    public SendConfirmToSitePostJob(ISitePostingService SitePostingService)
    {
        _sitePostingService = SitePostingService;
    }

    public async Task Execute()
    {
        await _sitePostingService.SendConfirmToAdmin();
    }
}
