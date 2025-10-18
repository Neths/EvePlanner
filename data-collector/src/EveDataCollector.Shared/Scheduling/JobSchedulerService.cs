using EveDataCollector.Core.Interfaces.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace EveDataCollector.Shared.Scheduling;

/// <summary>
/// Background service that executes scheduled jobs based on cron expressions
/// </summary>
public class JobSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerService> _logger;
    private readonly Dictionary<IScheduledJob, CrontabSchedule> _jobSchedules = new();
    private readonly Dictionary<IScheduledJob, DateTime> _nextRunTimes = new();

    public JobSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<JobSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scheduler Service is starting...");

        // Discover all registered scheduled jobs
        using var scope = _serviceProvider.CreateScope();
        var jobs = scope.ServiceProvider.GetServices<IScheduledJob>();

        foreach (var job in jobs)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(job.CronExpression);
                _jobSchedules[job] = schedule;
                _nextRunTimes[job] = schedule.GetNextOccurrence(DateTime.UtcNow);

                _logger.LogInformation(
                    "Registered job '{JobName}' with schedule '{CronExpression}'. Next run: {NextRun:yyyy-MM-dd HH:mm:ss} UTC",
                    job.JobName, job.CronExpression, _nextRunTimes[job]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register job '{JobName}' with expression '{CronExpression}'",
                    job.JobName, job.CronExpression);
            }
        }

        if (_jobSchedules.Count == 0)
        {
            _logger.LogWarning("No scheduled jobs found!");
        }
        else
        {
            _logger.LogInformation("Job Scheduler Service started with {Count} job(s)", _jobSchedules.Count);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Scheduler Service is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteJobsAsync(stoppingToken);

                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job scheduler loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Job Scheduler Service is stopping");
    }

    private async Task CheckAndExecuteJobsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var (job, schedule) in _jobSchedules)
        {
            var nextRun = _nextRunTimes[job];

            if (now >= nextRun)
            {
                _logger.LogInformation("Executing scheduled job '{JobName}' at {Time:yyyy-MM-dd HH:mm:ss} UTC",
                    job.JobName, now);

                try
                {
                    // Create a new scope for each job execution
                    using var scope = _serviceProvider.CreateScope();
                    var scopedJob = scope.ServiceProvider.GetRequiredService(job.GetType()) as IScheduledJob;

                    if (scopedJob != null)
                    {
                        await scopedJob.ExecuteAsync(cancellationToken);
                        _logger.LogInformation("Job '{JobName}' completed successfully", job.JobName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job '{JobName}' failed", job.JobName);
                }

                // Calculate next run time
                _nextRunTimes[job] = schedule.GetNextOccurrence(DateTime.UtcNow);
                _logger.LogInformation("Next run for '{JobName}': {NextRun:yyyy-MM-dd HH:mm:ss} UTC",
                    job.JobName, _nextRunTimes[job]);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scheduler Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
