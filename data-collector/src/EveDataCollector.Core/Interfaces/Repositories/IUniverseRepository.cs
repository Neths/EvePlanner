using EveDataCollector.Core.Models.Universe;

namespace EveDataCollector.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Universe data operations
/// </summary>
public interface IUniverseRepository
{
    // Categories
    Task<int> BulkInsertCategoriesAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<int> GetCategoriesCountAsync(CancellationToken cancellationToken = default);

    // Groups
    Task<int> BulkInsertGroupsAsync(IEnumerable<Group> groups, CancellationToken cancellationToken = default);
    Task<Group?> GetGroupAsync(int groupId, CancellationToken cancellationToken = default);
    Task<int> GetGroupsCountAsync(CancellationToken cancellationToken = default);

    // Types
    Task<int> BulkInsertTypesAsync(IEnumerable<ItemType> types, CancellationToken cancellationToken = default);
    Task<ItemType?> GetTypeAsync(int typeId, CancellationToken cancellationToken = default);
    Task<int> GetTypesCountAsync(CancellationToken cancellationToken = default);
    Task<List<int>> GetAllTypeIdsAsync(CancellationToken cancellationToken = default);

    // Regions
    Task<int> BulkInsertRegionsAsync(IEnumerable<Region> regions, CancellationToken cancellationToken = default);
    Task<Region?> GetRegionAsync(int regionId, CancellationToken cancellationToken = default);
    Task<int> GetRegionsCountAsync(CancellationToken cancellationToken = default);

    // Constellations
    Task<int> BulkInsertConstellationsAsync(IEnumerable<Constellation> constellations, CancellationToken cancellationToken = default);
    Task<Constellation?> GetConstellationAsync(int constellationId, CancellationToken cancellationToken = default);
    Task<int> GetConstellationsCountAsync(CancellationToken cancellationToken = default);

    // Systems
    Task<int> BulkInsertSystemsAsync(IEnumerable<SolarSystem> systems, CancellationToken cancellationToken = default);
    Task<SolarSystem?> GetSystemAsync(int systemId, CancellationToken cancellationToken = default);
    Task<int> GetSystemsCountAsync(CancellationToken cancellationToken = default);

    // Stations
    Task<int> BulkInsertStationsAsync(IEnumerable<Station> stations, CancellationToken cancellationToken = default);
    Task<Station?> GetStationAsync(int stationId, CancellationToken cancellationToken = default);
    Task<int> GetStationsCountAsync(CancellationToken cancellationToken = default);
}
