namespace EveDataCollector.Core.Interfaces.Jobs;

/// <summary>
/// Interface for scheduled jobs
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Job name for identification and logging
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Cron expression for scheduling (e.g., "0 0 * * *" for daily at midnight)
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Execute the job
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
