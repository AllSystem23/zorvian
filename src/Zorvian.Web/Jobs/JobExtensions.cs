using Hangfire;
using Hangfire.States;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Helper extension methods for scheduling Hangfire jobs
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Schedule a job to run at a specific date/time
    /// </summary>
    public static void ScheduleAt(this IRecurringJobManager manager, string jobId, DateTime runAt, Func<Task> job)
    {
        manager.AddOrUpdate(jobId, () => job(), Cron.Never);
    }

    /// <summary>
    /// Schedule a job to run after a delay
    /// </summary>
    public static void ScheduleIn(this IRecurringJobManager manager, string jobId, TimeSpan delay, Func<Task> job)
    {
        manager.AddOrUpdate(jobId, () => job(), Cron.Never);
    }

    /// <summary>
    /// Run a job immediately in the background
    /// </summary>
    public static void EnqueueNow(this IBackgroundJobClient client, Func<Task> job)
    {
        client.Enqueue(() => job());
    }

    /// <summary>
    /// Create a job chain (run sequentially)
    /// </summary>
    public static string CreateChain(this IBackgroundJobClient client, params Func<Task>[] jobs)
    {
        if (jobs.Length == 0) return string.Empty;
        var parentId = client.Enqueue(() => jobs[0]());
        for (int i = 1; i < jobs.Length; i++)
        {
            parentId = client.ContinueJobWith(parentId, () => jobs[i]());
        }
        return parentId;
    }

    /// <summary>
    /// Cancel a job by ID
    /// </summary>
    public static bool Cancel(this IBackgroundJobClient client, string jobId)
    {
        return client.Delete(jobId);
    }
}


