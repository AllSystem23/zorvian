using Hangfire;
using Nexora.Application.Interfaces;
using Nexora.Application.Jobs;

namespace Nexora.Infrastructure.Services;

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
