using ContainerFather.Bot.Services;
using ContainerFather.Bot.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace ContainerFather.Bot.BackgroundJobs.Jobs;

[Queue("default")]
[AutomaticRetry(Attempts = 0)]
public class SendWeeklyMessageJob
{
    private readonly IBroadcastService _broadcastService;

    public SendWeeklyMessageJob(IBroadcastService broadcastService)
    {
        _broadcastService = broadcastService;
    }
    public async Task Execute()
    {
        await _broadcastService.SendWeeklyBroadcastMessageAsync(2, CancellationToken.None);
    }
}