using ContainerFather.Bot.Services;
using ContainerFather.Bot.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace ContainerFather.Bot.BackgroundJobs.Jobs;

[Queue("default")]
[AutomaticRetry(Attempts = 0)]
public class SendDailyMessageJob
{
    private readonly IBroadcastService _broadcastService;

    public SendDailyMessageJob(IBroadcastService broadcastService)
    {
        _broadcastService = broadcastService;
    }

    public async Task Execute()
    {
        await _broadcastService.SendDailyBroadcastMessageAsync(2, CancellationToken.None);
        await _broadcastService.SendDailyChanelBroadcastMessageAsync(6, CancellationToken.None);
    }
}