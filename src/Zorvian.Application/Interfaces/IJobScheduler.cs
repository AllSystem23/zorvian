namespace Zorvian.Application.Interfaces;

public interface IJobScheduler
{
    void EnqueueOcrJob(Guid permissionRequestId);
}
