using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Infrastructure.Collectors;
using EveDataCollector.Infrastructure.ESI;
using EveDataCollector.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

// Register HttpClient for ESI with Polly retry policy
builder.Services.AddHttpClient<EsiClient>(client =>
{
    var esiConfig = builder.Configuration.GetSection("EsiClient");
    client.BaseAddress = new Uri(esiConfig["BaseUrl"] ?? "https://esi.evetech.net/latest");
    client.DefaultRequestHeaders.Add("User-Agent", esiConfig["UserAgent"] ?? "EveDataCollector/0.1.0");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(esiConfig["Timeout"] ?? "30"));
})
.AddPolicyHandler(GetRetryPolicy());

// Register database connection factory
builder.Services.AddSingleton<Func<NpgsqlConnection>>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not found");
    return () => new NpgsqlConnection(connectionString);
});

// Register repositories
builder.Services.AddScoped<IUniverseRepository, UniverseRepository>();

// Register collectors
builder.Services.AddScoped<UniverseCollector>();

// Build and run
var host = builder.Build();

try
{
    Log.Information("Starting EVE Data Collector...");

    // Test database connection
    var dbConnectionFactory = host.Services.GetRequiredService<Func<NpgsqlConnection>>();
    await using (var connection = dbConnectionFactory())
    {
        await connection.OpenAsync();
        Log.Information("Database connection successful: {Database}", connection.Database);
    }

    // Test ESI client
    var esiClient = host.Services.GetRequiredService<EsiClient>();
    var categories = await esiClient.Universe.GetCategoriesAsync();
    Log.Information("ESI client test successful: Found {Count} categories", categories.Count);

    // Ask user if they want to collect Universe data
    Log.Information("");
    Log.Information("Do you want to collect Universe data? (y/n)");
    var answer = Console.ReadLine()?.Trim().ToLower();

    if (answer == "y" || answer == "yes")
    {
        using var scope = host.Services.CreateScope();
        var collector = scope.ServiceProvider.GetRequiredService<UniverseCollector>();

        await collector.CollectAllAsync();

        Log.Information("Universe data collection completed successfully!");
    }
    else
    {
        Log.Information("Skipping Universe data collection.");
    }

    Log.Information("");
    Log.Information("All systems operational. Application will now exit.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

// Polly retry policy for ESI HTTP calls
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Log.Warning("Retry {RetryAttempt} after {Delay}s due to {Reason}",
                    retryAttempt, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
            });
}
