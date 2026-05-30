namespace Nexora.Application.Interfaces;

public interface IJobScheduler
{
    void EnqueueOcrJob(Guid permissionRequestId);
}
