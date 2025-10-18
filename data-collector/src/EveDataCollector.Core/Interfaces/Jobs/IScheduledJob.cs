namespace EveDataCollector.Core.Interfaces.Jobs;

/// <summary>
/// Interface for scheduled jobs that run on a cron schedule
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Name of the job for logging purposes
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Cron expression defining when the job should run
    /// Format: minute hour day month dayofweek (5 fields)
    /// Example: "0 2 * * *" = Every day at 2 AM
    /// Example: "*/15 * * * *" = Every 15 minutes
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Executes the scheduled job
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
