using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Interfaces.Jobs;
using EveDataCollector.Infrastructure.Auth;
using EveDataCollector.Infrastructure.Collectors;
using EveDataCollector.Infrastructure.ESI;
using EveDataCollector.Infrastructure.Jobs;
using EveDataCollector.Infrastructure.Repositories;
using EveDataCollector.Shared.Auth;
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
builder.Services.AddScoped<MarketOrdersCollector>();
builder.Services.AddScoped<MarketPricesCollector>();
builder.Services.AddScoped<MarketHistoryCollector>();
builder.Services.AddScoped<MarketDataCollector>();

// Register OAuth services
builder.Services.AddScoped<CharacterAuthService>();

// Register scheduled jobs
builder.Services.AddScoped<IScheduledJob, UniverseCollectionJob>();
builder.Services.AddScoped<IScheduledJob, MarketCollectionJob>();

// Register background services
builder.Services.AddHostedService<TokenRefreshService>();

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

    // Main menu
    while (true)
    {
        Log.Information("");
        Log.Information("=== EVE Data Collector Menu ===");
        Log.Information("1. Collect Universe data");
        Log.Information("2. Authorize character (OAuth)");
        Log.Information("3. List authorized characters");
        Log.Information("4. Collect character data");
        Log.Information("5. Collect market data");
        Log.Information("6. Exit");
        Log.Information("");
        Console.Write("Select an option: ");

        var choice = Console.ReadLine()?.Trim();

        if (choice == "6")
        {
            Log.Information("Exiting...");
            break;
        }

        try
        {
            switch (choice)
            {
                case "1":
                    await CollectUniverseDataAsync(host);
                    break;

                case "2":
                    await AuthorizeCharacterAsync(host);
                    break;

                case "3":
                    await ListCharactersAsync(host);
                    break;

                case "4":
                    await CollectCharacterDataAsync(host);
                    break;

                case "5":
                    await CollectMarketDataAsync(host);
                    break;

                default:
                    Log.Warning("Invalid option. Please select 1-6.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing command");
        }
    }

    Log.Information("All systems operational. Application exiting.");
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

// Menu command handlers
static async Task CollectUniverseDataAsync(IHost host)
{
    Log.Information("Starting Universe data collection...");

    using var scope = host.Services.CreateScope();
    var collector = scope.ServiceProvider.GetRequiredService<UniverseCollector>();

    await collector.CollectAllAsync();

    Log.Information("Universe data collection completed successfully!");
}

static async Task AuthorizeCharacterAsync(IHost host)
{
    var config = host.Services.GetRequiredService<IConfiguration>();
    var oauthConfig = config.GetSection("EsiOAuth");

    var clientId = oauthConfig["ClientId"];
    var clientSecret = oauthConfig["ClientSecret"];

    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
    {
        Log.Error("OAuth configuration missing! Please configure ClientId and ClientSecret in appsettings.json");
        Log.Information("See docs/OAUTH_SETUP.md for setup instructions");
        return;
    }

    using var scope = host.Services.CreateScope();
    var authService = scope.ServiceProvider.GetRequiredService<CharacterAuthService>();

    // Register or get application
    var application = await authService.RegisterApplicationAsync(
        oauthConfig["ApplicationName"] ?? "EVE Data Collector",
        clientId,
        clientSecret,
        oauthConfig["CallbackUrl"] ?? "http://localhost:8080/callback",
        oauthConfig.GetSection("Scopes").Get<string[]>() ?? Array.Empty<string>());

    Log.Information("OAuth application registered: {AppName}", application.Name);

    // Start authorization flow
    var result = await authService.AuthorizeCharacterAsync(
        application,
        application.Scopes);

    if (result.HasValue)
    {
        Log.Information("");
        Log.Information("=== Character authorized successfully! ===");
        Log.Information("Character: {Name} ({Id})", result.Value.Character.CharacterName, result.Value.Character.CharacterId);
        if (result.Value.Character.CorporationName != null)
            Log.Information("Corporation: {Corp}", result.Value.Character.CorporationName);
        if (result.Value.Character.AllianceName != null)
            Log.Information("Alliance: {Alliance}", result.Value.Character.AllianceName);
        Log.Information("Token expires: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC", result.Value.Token.ExpiresAt);
    }
    else
    {
        Log.Warning("Character authorization failed or was cancelled");
    }
}

static async Task ListCharactersAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();

    var characters = await authRepository.GetAllCharactersAsync();

    if (characters.Count == 0)
    {
        Log.Information("No authorized characters found");
        return;
    }

    Log.Information("");
    Log.Information("=== Authorized Characters ({Count}) ===", characters.Count);
    foreach (var character in characters)
    {
        Log.Information("- {Name} ({Id})", character.CharacterName, character.CharacterId);
        if (character.CorporationName != null)
            Log.Information("  Corporation: {Corp}", character.CorporationName);
        if (character.AllianceName != null)
            Log.Information("  Alliance: {Alliance}", character.AllianceName);
    }
}

static async Task CollectCharacterDataAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
    var characterCollector = scope.ServiceProvider.GetRequiredService<CharacterDataCollector>();

    var characters = await authRepository.GetAllCharactersAsync();

    if (characters.Count == 0)
    {
        Log.Warning("No authorized characters found. Please authorize a character first (option 2).");
        return;
    }

    Log.Information("");
    Log.Information("Select a character to collect data:");
    for (int i = 0; i < characters.Count; i++)
    {
        Log.Information("{Index}. {Name} ({Id})", i + 1, characters[i].CharacterName, characters[i].CharacterId);
    }
    Log.Information("0. Collect all characters");
    Log.Information("");
    Console.Write("Select character: ");

    var input = Console.ReadLine()?.Trim();
    if (!int.TryParse(input, out var selection))
    {
        Log.Warning("Invalid selection");
        return;
    }

    var config = host.Services.GetRequiredService<IConfiguration>();
    var oauthConfig = config.GetSection("EsiOAuth");
    var clientId = oauthConfig["ClientId"];

    if (string.IsNullOrWhiteSpace(clientId))
    {
        Log.Error("OAuth ClientId not configured");
        return;
    }

    // Get application ID
    var application = await authRepository.GetApplicationByClientIdAsync(clientId);
    if (application == null)
    {
        Log.Error("OAuth application not found in database");
        return;
    }

    if (selection == 0)
    {
        // Collect all characters
        Log.Information("Collecting data for all {Count} character(s)...", characters.Count);
        foreach (var character in characters)
        {
            try
            {
                await characterCollector.CollectAllAsync(character.CharacterId, application.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to collect data for character {CharacterId}", character.CharacterId);
            }
        }
    }
    else if (selection > 0 && selection <= characters.Count)
    {
        // Collect specific character
        var character = characters[selection - 1];
        await characterCollector.CollectAllAsync(character.CharacterId, application.Id);
    }
    else
    {
        Log.Warning("Invalid selection");
    }
}

static async Task CollectMarketDataAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var marketCollector = scope.ServiceProvider.GetRequiredService<MarketDataCollector>();
    var config = host.Services.GetRequiredService<IConfiguration>();

    Log.Information("");
    Log.Information("=== Market Data Collection ===");
    Log.Information("1. Collect all market data (prices + orders for major trade hubs)");
    Log.Information("2. Collect market prices only");
    Log.Information("3. Collect market orders for specific region");
    Log.Information("4. Back to main menu");
    Log.Information("");
    Console.Write("Select an option: ");

    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                // Get regions from config or use defaults
                var regionsConfig = config.GetSection("Scheduling:MarketCollection:Regions").Get<int[]>();
                var regions = regionsConfig ?? new[]
                {
                    10000002,  // The Forge (Jita)
                    10000043,  // Domain (Amarr)
                    10000032,  // Sinq Laison (Dodixie)
                    10000030,  // Heimatar (Rens)
                    10000042   // Metropolis (Hek)
                };

                Log.Information("Collecting market data for {Count} regions...", regions.Length);
                await marketCollector.CollectAllMarketDataAsync(regions);
                Log.Information("Market data collection completed!");
                break;

            case "2":
                Log.Information("Collecting global market prices...");
                await marketCollector.CollectMarketPricesAsync();
                Log.Information("Market prices collection completed!");
                break;

            case "3":
                Log.Information("");
                Log.Information("Major regions:");
                Log.Information("10000002 - The Forge (Jita)");
                Log.Information("10000043 - Domain (Amarr)");
                Log.Information("10000032 - Sinq Laison (Dodixie)");
                Log.Information("10000030 - Heimatar (Rens)");
                Log.Information("10000042 - Metropolis (Hek)");
                Log.Information("");
                Console.Write("Enter region ID: ");

                var regionInput = Console.ReadLine()?.Trim();
                if (int.TryParse(regionInput, out var regionId))
                {
                    Log.Information("Collecting market orders for region {RegionId}...", regionId);
                    await marketCollector.CollectMarketOrdersAsync(new[] { regionId });
                    Log.Information("Market orders collection completed!");
                }
                else
                {
                    Log.Warning("Invalid region ID");
                }
                break;

            case "4":
                return;

            default:
                Log.Warning("Invalid option. Please select 1-4.");
                break;
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error collecting market data");
    }
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
