using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Universe;
using EveDataCollector.Infrastructure.ESI;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collector for Universe static data from ESI
/// </summary>
public class UniverseCollector
{
    private readonly EsiClient _esiClient;
    private readonly IUniverseRepository _repository;
    private readonly ILogger<UniverseCollector> _logger;

    public UniverseCollector(
        EsiClient esiClient,
        IUniverseRepository repository,
        ILogger<UniverseCollector> logger)
    {
        _esiClient = esiClient;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Collect all categories from ESI and store in database
    /// </summary>
    public async Task<int> CollectCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting categories collection...");

        // Get all category IDs
        var categoryIds = await _esiClient.Universe.GetCategoriesAsync(cancellationToken);
        _logger.LogInformation("Found {Count} categories", categoryIds.Count);

        // Fetch each category details
        var categories = new List<Category>();
        foreach (var categoryId in categoryIds)
        {
            try
            {
                var categoryInfo = await _esiClient.Universe.GetCategoryAsync(categoryId, cancellationToken);
                categories.Add(new Category
                {
                    CategoryId = categoryInfo.CategoryId,
                    Name = categoryInfo.Name,
                    Published = categoryInfo.Published,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch category {CategoryId}", categoryId);
            }
        }

        // Bulk insert
        var inserted = await _repository.BulkInsertCategoriesAsync(categories, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} categories", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all groups from ESI and store in database
    /// </summary>
    public async Task<int> CollectGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting groups collection...");

        // Get all group IDs
        var groupIds = await _esiClient.Universe.GetGroupsAsync(cancellationToken);
        _logger.LogInformation("Found {Count} groups", groupIds.Count);

        // Fetch each group details
        var groups = new List<Group>();
        foreach (var groupId in groupIds)
        {
            try
            {
                var groupInfo = await _esiClient.Universe.GetGroupAsync(groupId, cancellationToken);
                groups.Add(new Group
                {
                    GroupId = groupInfo.GroupId,
                    Name = groupInfo.Name,
                    CategoryId = groupInfo.CategoryId,
                    Published = groupInfo.Published,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch group {GroupId}", groupId);
            }
        }

        // Bulk insert
        var inserted = await _repository.BulkInsertGroupsAsync(groups, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} groups", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect types from groups and store in database
    /// </summary>
    public async Task<int> CollectTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting types collection...");

        // Get all group IDs first
        var groupIds = await _esiClient.Universe.GetGroupsAsync(cancellationToken);

        // Collect all type IDs from groups
        var allTypeIds = new HashSet<int>();
        foreach (var groupId in groupIds)
        {
            try
            {
                var groupInfo = await _esiClient.Universe.GetGroupAsync(groupId, cancellationToken);
                foreach (var typeId in groupInfo.Types)
                {
                    allTypeIds.Add(typeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch group {GroupId} for types", groupId);
            }
        }

        _logger.LogInformation("Found {Count} unique types across all groups", allTypeIds.Count);

        // Fetch each type details (in batches to avoid overwhelming the API)
        var types = new List<ItemType>();
        var processed = 0;
        foreach (var typeId in allTypeIds)
        {
            try
            {
                var typeInfo = await _esiClient.Universe.GetTypeAsync(typeId, cancellationToken);
                types.Add(new ItemType
                {
                    TypeId = typeInfo.TypeId,
                    Name = typeInfo.Name,
                    Description = typeInfo.Description,
                    GroupId = typeInfo.GroupId,
                    MarketGroupId = typeInfo.MarketGroupId,
                    Volume = typeInfo.Volume,
                    Capacity = typeInfo.Capacity,
                    PackagedVolume = typeInfo.PackagedVolume,
                    Mass = typeInfo.Mass,
                    PortionSize = typeInfo.PortionSize,
                    Radius = typeInfo.Radius,
                    Published = typeInfo.Published,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                processed++;
                if (processed % 100 == 0)
                {
                    _logger.LogInformation("Processed {Processed}/{Total} types...", processed, allTypeIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch type {TypeId}", typeId);
            }
        }

        // Bulk insert
        var inserted = await _repository.BulkInsertTypesAsync(types, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} types", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all regions from ESI and store in database
    /// </summary>
    public async Task<int> CollectRegionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting regions collection...");

        var regionIds = await _esiClient.Universe.GetRegionsAsync(cancellationToken);
        _logger.LogInformation("Found {Count} regions", regionIds.Count);

        var regions = new List<Region>();
        foreach (var regionId in regionIds)
        {
            try
            {
                var regionInfo = await _esiClient.Universe.GetRegionAsync(regionId, cancellationToken);
                regions.Add(new Region
                {
                    RegionId = regionInfo.RegionId,
                    Name = regionInfo.Name,
                    Description = regionInfo.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch region {RegionId}", regionId);
            }
        }

        var inserted = await _repository.BulkInsertRegionsAsync(regions, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} regions", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all constellations from regions and store in database
    /// </summary>
    public async Task<int> CollectConstellationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting constellations collection...");

        var regionIds = await _esiClient.Universe.GetRegionsAsync(cancellationToken);

        var allConstellationIds = new HashSet<int>();
        foreach (var regionId in regionIds)
        {
            try
            {
                var regionInfo = await _esiClient.Universe.GetRegionAsync(regionId, cancellationToken);
                foreach (var constellationId in regionInfo.Constellations)
                {
                    allConstellationIds.Add(constellationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch region {RegionId} for constellations", regionId);
            }
        }

        _logger.LogInformation("Found {Count} constellations", allConstellationIds.Count);

        var constellations = new List<Constellation>();
        foreach (var constellationId in allConstellationIds)
        {
            try
            {
                var constInfo = await _esiClient.Universe.GetConstellationAsync(constellationId, cancellationToken);
                constellations.Add(new Constellation
                {
                    ConstellationId = constInfo.ConstellationId,
                    Name = constInfo.Name,
                    RegionId = constInfo.RegionId,
                    PositionX = constInfo.Position?.X,
                    PositionY = constInfo.Position?.Y,
                    PositionZ = constInfo.Position?.Z,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch constellation {ConstellationId}", constellationId);
            }
        }

        var inserted = await _repository.BulkInsertConstellationsAsync(constellations, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} constellations", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all systems from constellations and store in database
    /// </summary>
    public async Task<int> CollectSystemsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting systems collection...");

        var regionIds = await _esiClient.Universe.GetRegionsAsync(cancellationToken);

        var allSystemIds = new HashSet<int>();
        foreach (var regionId in regionIds)
        {
            try
            {
                var regionInfo = await _esiClient.Universe.GetRegionAsync(regionId, cancellationToken);
                foreach (var constellationId in regionInfo.Constellations)
                {
                    try
                    {
                        var constInfo = await _esiClient.Universe.GetConstellationAsync(constellationId, cancellationToken);
                        foreach (var systemId in constInfo.Systems)
                        {
                            allSystemIds.Add(systemId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch constellation {ConstellationId} for systems", constellationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch region {RegionId} for systems", regionId);
            }
        }

        _logger.LogInformation("Found {Count} systems", allSystemIds.Count);

        var systems = new List<SolarSystem>();
        var processed = 0;
        foreach (var systemId in allSystemIds)
        {
            try
            {
                var systemInfo = await _esiClient.Universe.GetSystemAsync(systemId, cancellationToken);
                systems.Add(new SolarSystem
                {
                    SystemId = systemInfo.SystemId,
                    Name = systemInfo.Name,
                    ConstellationId = systemInfo.ConstellationId,
                    PositionX = systemInfo.Position?.X,
                    PositionY = systemInfo.Position?.Y,
                    PositionZ = systemInfo.Position?.Z,
                    SecurityStatus = systemInfo.SecurityStatus,
                    SecurityClass = systemInfo.SecurityClass,
                    StarId = systemInfo.StarId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                processed++;
                if (processed % 100 == 0)
                {
                    _logger.LogInformation("Processed {Processed}/{Total} systems...", processed, allSystemIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch system {SystemId}", systemId);
            }
        }

        var inserted = await _repository.BulkInsertSystemsAsync(systems, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} systems", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all stations from systems and store in database
    /// </summary>
    public async Task<int> CollectStationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting stations collection...");

        var regionIds = await _esiClient.Universe.GetRegionsAsync(cancellationToken);

        var allStationIds = new HashSet<int>();
        foreach (var regionId in regionIds)
        {
            try
            {
                var regionInfo = await _esiClient.Universe.GetRegionAsync(regionId, cancellationToken);
                foreach (var constellationId in regionInfo.Constellations)
                {
                    try
                    {
                        var constInfo = await _esiClient.Universe.GetConstellationAsync(constellationId, cancellationToken);
                        foreach (var systemId in constInfo.Systems)
                        {
                            try
                            {
                                var systemInfo = await _esiClient.Universe.GetSystemAsync(systemId, cancellationToken);
                                if (systemInfo.Stations != null)
                                {
                                    foreach (var stationId in systemInfo.Stations)
                                    {
                                        allStationIds.Add(stationId);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to fetch system {SystemId} for stations", systemId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch constellation {ConstellationId} for stations", constellationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch region {RegionId} for stations", regionId);
            }
        }

        _logger.LogInformation("Found {Count} stations", allStationIds.Count);

        var stations = new List<Station>();
        foreach (var stationId in allStationIds)
        {
            try
            {
                var stationInfo = await _esiClient.Universe.GetStationAsync(stationId, cancellationToken);
                stations.Add(new Station
                {
                    StationId = stationInfo.StationId,
                    Name = stationInfo.Name,
                    SystemId = stationInfo.SystemId,
                    TypeId = stationInfo.TypeId,
                    Owner = stationInfo.Owner,
                    PositionX = stationInfo.Position?.X,
                    PositionY = stationInfo.Position?.Y,
                    PositionZ = stationInfo.Position?.Z,
                    RaceId = stationInfo.RaceId,
                    Services = stationInfo.Services?.ToArray(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch station {StationId}", stationId);
            }
        }

        var inserted = await _repository.BulkInsertStationsAsync(stations, cancellationToken);
        _logger.LogInformation("Inserted/Updated {Count} stations", inserted);

        return inserted;
    }

    /// <summary>
    /// Collect all Universe data in the correct order
    /// </summary>
    public async Task CollectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting complete Universe data collection ===");

        await CollectCategoriesAsync(cancellationToken);
        await CollectGroupsAsync(cancellationToken);
        await CollectTypesAsync(cancellationToken);
        await CollectRegionsAsync(cancellationToken);
        await CollectConstellationsAsync(cancellationToken);
        await CollectSystemsAsync(cancellationToken);
        await CollectStationsAsync(cancellationToken);

        _logger.LogInformation("=== Universe data collection completed ===");

        // Log final counts
        var categoriesCount = await _repository.GetCategoriesCountAsync(cancellationToken);
        var groupsCount = await _repository.GetGroupsCountAsync(cancellationToken);
        var typesCount = await _repository.GetTypesCountAsync(cancellationToken);
        var regionsCount = await _repository.GetRegionsCountAsync(cancellationToken);
        var constellationsCount = await _repository.GetConstellationsCountAsync(cancellationToken);
        var systemsCount = await _repository.GetSystemsCountAsync(cancellationToken);
        var stationsCount = await _repository.GetStationsCountAsync(cancellationToken);

        _logger.LogInformation(
            "Final counts: Categories={Categories}, Groups={Groups}, Types={Types}, Regions={Regions}, Constellations={Constellations}, Systems={Systems}, Stations={Stations}",
            categoriesCount, groupsCount, typesCount, regionsCount, constellationsCount, systemsCount, stationsCount);
    }
}
