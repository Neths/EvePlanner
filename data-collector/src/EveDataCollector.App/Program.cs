using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Infrastructure.Auth;
using EveDataCollector.Infrastructure.Collectors;
using EveDataCollector.Infrastructure.ESI;
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

// Register repositories
builder.Services.AddScoped<IUniverseRepository, UniverseRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICharacterDataRepository, CharacterDataRepository>();

// Register collectors
builder.Services.AddScoped<UniverseCollector>();
builder.Services.AddScoped<CharacterSkillsCollector>();
builder.Services.AddScoped<CharacterAssetsCollector>();
builder.Services.AddScoped<CharacterWalletCollector>();
builder.Services.AddScoped<CharacterDataCollector>();

// Register OAuth services
builder.Services.AddScoped<CharacterAuthService>();

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
        Log.Information("5. Exit");
        Log.Information("");
        Console.Write("Select an option: ");

        var choice = Console.ReadLine()?.Trim();

        if (choice == "5")
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

                default:
                    Log.Warning("Invalid option. Please select 1-5.");
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
