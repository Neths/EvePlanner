using EveDataCollector.App.Data;
using EveDataCollector.App.Services;
using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Infrastructure.Auth;
using EveDataCollector.Infrastructure.Collectors;
using EveDataCollector.Infrastructure.ESI;
using EveDataCollector.Infrastructure.Repositories;
using EveDataCollector.Shared.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using TickerQ;
using TickerQ.Dashboard;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL DbContext for TickerQ
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure TickerQ
builder.Services.AddTickerQ(options =>
{
    // Set max concurrency to 1 to ensure only one job can run at a time
    options.SetMaxConcurrency(1);
    options.AddOperationalStore<ApplicationDbContext>(efOpt =>
    {
        efOpt.UseModelCustomizerForMigrations();
    });
    options.AddDashboard(uiopt =>
    {
        uiopt.BasePath = "/tickerq";
    });
});

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
    return () => new NpgsqlConnection(connectionString);
});

// Register HttpClient for OAuth
builder.Services.AddHttpClient<IEsiOAuthClient, EsiOAuthClient>();

// Register HttpClient for authenticated ESI
builder.Services.AddHttpClient<AuthenticatedEsiClient>(client =>
{
    var esiConfig = builder.Configuration.GetSection("EsiClient");
    client.BaseAddress = new Uri(esiConfig["BaseUrl"] ?? "https://esi.evetech.net/latest");
    client.DefaultRequestHeaders.Add("User-Agent", esiConfig["UserAgent"] ?? "EveDataCollector/0.1.0");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(esiConfig["Timeout"] ?? "30"));
})
.AddPolicyHandler(GetRetryPolicy());

// Register HttpClient for market collectors
builder.Services.AddHttpClient<MarketOrdersCollector>(client =>
{
    var esiConfig = builder.Configuration.GetSection("EsiClient");
    client.BaseAddress = new Uri(esiConfig["BaseUrl"] ?? "https://esi.evetech.net/latest");
    client.DefaultRequestHeaders.Add("User-Agent", esiConfig["UserAgent"] ?? "EveDataCollector/0.1.0");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(esiConfig["Timeout"] ?? "30"));
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<MarketPricesCollector>(client =>
{
    var esiConfig = builder.Configuration.GetSection("EsiClient");
    client.BaseAddress = new Uri(esiConfig["BaseUrl"] ?? "https://esi.evetech.net/latest");
    client.DefaultRequestHeaders.Add("User-Agent", esiConfig["UserAgent"] ?? "EveDataCollector/0.1.0");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(esiConfig["Timeout"] ?? "30"));
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<MarketHistoryCollector>(client =>
{
    var esiConfig = builder.Configuration.GetSection("EsiClient");
    client.BaseAddress = new Uri(esiConfig["BaseUrl"] ?? "https://esi.evetech.net/latest");
    client.DefaultRequestHeaders.Add("User-Agent", esiConfig["UserAgent"] ?? "EveDataCollector/0.1.0");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(esiConfig["Timeout"] ?? "30"));
})
.AddPolicyHandler(GetRetryPolicy());

// Register repositories
builder.Services.AddScoped<IUniverseRepository, UniverseRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICharacterDataRepository, CharacterDataRepository>();
builder.Services.AddScoped<IMarketRepository, MarketRepository>();

// Register collectors
builder.Services.AddScoped<UniverseCollector>();
builder.Services.AddScoped<CharacterSkillsCollector>();
builder.Services.AddScoped<CharacterAssetsCollector>();
builder.Services.AddScoped<CharacterWalletCollector>();
builder.Services.AddScoped<CharacterDataCollector>();
// Note: MarketOrdersCollector, MarketPricesCollector, and MarketHistoryCollector are registered via AddHttpClient above
builder.Services.AddScoped<MarketDataCollector>();

// Register OAuth services
builder.Services.AddScoped<CharacterAuthService>();

// Register scheduled collection service for TickerQ
builder.Services.AddScoped<ScheduledCollectionService>();

// Register background services
builder.Services.AddHostedService<TokenRefreshService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

// Configure TickerQ processor and dashboard
app.UseTickerQ();

try
{
    Log.Information("Starting EVE Data Collector Web API...");

    // Ensure database is created and migrated
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        Log.Information("TickerQ database initialized");
    }

    // Test database connection
    var dbConnectionFactory = app.Services.GetRequiredService<Func<NpgsqlConnection>>();
    await using (var connection = dbConnectionFactory())
    {
        await connection.OpenAsync();
        Log.Information("Database connection successful: {Database}", connection.Database);
    }

    // Test ESI client
    var esiClient = app.Services.GetRequiredService<EsiClient>();
    var categories = await esiClient.Universe.GetCategoriesAsync();
    Log.Information("ESI client test successful: Found {Count} categories", categories.Count);

    Log.Information("EVE Data Collector Web API started successfully");
    Log.Information("TickerQ Dashboard available at: /tickerq");
    Log.Information("Swagger UI available at: /swagger");
    Log.Information("Scheduled jobs are managed by TickerQ automatically");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

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
