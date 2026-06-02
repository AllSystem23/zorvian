using Hangfire;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Jobs;

namespace Zorvian.Infrastructure.Services;

public sealed class HangfireJobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void EnqueueOcrJob(Guid permissionRequestId)
    {
        _backgroundJobClient.Enqueue<OcrProcessingJob>(j => j.RunAsync(permissionRequestId));
    }
}
