using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EveDataCollector.Infrastructure.ESI;

/// <summary>
/// Manual ESI client for EVE Online API
/// </summary>
public class EsiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EsiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    // Universe Endpoints
    public Universe Universe => new(_httpClient, _jsonOptions);
}

/// <summary>
/// Universe-related ESI endpoints
/// </summary>
public class Universe
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public Universe(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<List<int>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/universe/categories/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<int>>(_jsonOptions, cancellationToken)
               ?? new List<int>();
    }

    public async Task<CategoryInfo> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/categories/{categoryId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Category {categoryId} not found");
    }

    public async Task<List<int>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/universe/groups/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<int>>(_jsonOptions, cancellationToken)
               ?? new List<int>();
    }

    public async Task<GroupInfo> GetGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/groups/{groupId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GroupInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Group {groupId} not found");
    }

    public async Task<TypeInfo> GetTypeAsync(int typeId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/types/{typeId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TypeInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Type {typeId} not found");
    }

    public async Task<List<int>> GetRegionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/universe/regions/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<int>>(_jsonOptions, cancellationToken)
               ?? new List<int>();
    }

    public async Task<RegionInfo> GetRegionAsync(int regionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/regions/{regionId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegionInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Region {regionId} not found");
    }

    public async Task<ConstellationInfo> GetConstellationAsync(int constellationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/constellations/{constellationId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConstellationInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Constellation {constellationId} not found");
    }

    public async Task<SystemInfo> GetSystemAsync(int systemId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/systems/{systemId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SystemInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"System {systemId} not found");
    }

    public async Task<StationInfo> GetStationAsync(int stationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/universe/stations/{stationId}/", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StationInfo>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"Station {stationId} not found");
    }
}

// DTOs for ESI responses
public record CategoryInfo(
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("groups")] List<int> Groups
);

public record GroupInfo(
    [property: JsonPropertyName("group_id")] int GroupId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("category_id")] int CategoryId,
    [property: JsonPropertyName("types")] List<int> Types
);

public record TypeInfo(
    [property: JsonPropertyName("type_id")] int TypeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("group_id")] int GroupId,
    [property: JsonPropertyName("market_group_id")] int? MarketGroupId,
    [property: JsonPropertyName("volume")] double? Volume,
    [property: JsonPropertyName("capacity")] double? Capacity,
    [property: JsonPropertyName("packaged_volume")] double? PackagedVolume,
    [property: JsonPropertyName("mass")] double? Mass,
    [property: JsonPropertyName("portion_size")] int? PortionSize,
    [property: JsonPropertyName("radius")] double? Radius,
    [property: JsonPropertyName("published")] bool Published
);

public record RegionInfo(
    [property: JsonPropertyName("region_id")] int RegionId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("constellations")] List<int> Constellations
);

public record ConstellationInfo(
    [property: JsonPropertyName("constellation_id")] int ConstellationId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region_id")] int RegionId,
    [property: JsonPropertyName("position")] Position? Position,
    [property: JsonPropertyName("systems")] List<int> Systems
);

public record SystemInfo(
    [property: JsonPropertyName("system_id")] int SystemId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("constellation_id")] int ConstellationId,
    [property: JsonPropertyName("position")] Position? Position,
    [property: JsonPropertyName("security_status")] double SecurityStatus,
    [property: JsonPropertyName("security_class")] string? SecurityClass,
    [property: JsonPropertyName("star_id")] int? StarId,
    [property: JsonPropertyName("stations")] List<int>? Stations
);

public record StationInfo(
    [property: JsonPropertyName("station_id")] int StationId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("system_id")] int SystemId,
    [property: JsonPropertyName("type_id")] int TypeId,
    [property: JsonPropertyName("owner")] int? Owner,
    [property: JsonPropertyName("position")] Position? Position,
    [property: JsonPropertyName("race_id")] int? RaceId,
    [property: JsonPropertyName("services")] List<string>? Services
);

public record Position(
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("z")] double Z
);
