using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;

namespace EveDataCollector.App.Data;

/// <summary>
/// DbContext for TickerQ job scheduling
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TickerQ tables
        modelBuilder.ApplyConfiguration(new TimeTickerConfigurations());
        modelBuilder.ApplyConfiguration(new CronTickerConfigurations());
        modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations());
    }
}
