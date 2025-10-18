using Dapper;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Universe;
using Npgsql;

namespace EveDataCollector.Infrastructure.Repositories;

/// <summary>
/// Dapper-based repository for Universe data
/// </summary>
public class UniverseRepository : IUniverseRepository
{
    private readonly Func<NpgsqlConnection> _connectionFactory;

    public UniverseRepository(Func<NpgsqlConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    #region Categories

    public async Task<int> BulkInsertCategoriesAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO categories (category_id, name, published, created_at, updated_at)
            VALUES (@CategoryId, @Name, @Published, @CreatedAt, @UpdatedAt)
            ON CONFLICT (category_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                published = EXCLUDED.published,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, categories, cancellationToken: cancellationToken));
    }

    public async Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM categories WHERE category_id = @CategoryId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Category>(new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetCategoriesCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM categories";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion

    #region Groups

    public async Task<int> BulkInsertGroupsAsync(IEnumerable<Group> groups, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO groups (group_id, name, category_id, published, created_at, updated_at)
            VALUES (@GroupId, @Name, @CategoryId, @Published, @CreatedAt, @UpdatedAt)
            ON CONFLICT (group_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                category_id = EXCLUDED.category_id,
                published = EXCLUDED.published,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, groups, cancellationToken: cancellationToken));
    }

    public async Task<Group?> GetGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM groups WHERE group_id = @GroupId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Group>(new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetGroupsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM groups";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion

    #region Types

    public async Task<int> BulkInsertTypesAsync(IEnumerable<ItemType> types, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO types (type_id, name, description, group_id, market_group_id, volume, capacity,
                             packaged_volume, mass, portion_size, radius, published, created_at, updated_at)
            VALUES (@TypeId, @Name, @Description, @GroupId, @MarketGroupId, @Volume, @Capacity,
                    @PackagedVolume, @Mass, @PortionSize, @Radius, @Published, @CreatedAt, @UpdatedAt)
            ON CONFLICT (type_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                group_id = EXCLUDED.group_id,
                market_group_id = EXCLUDED.market_group_id,
                volume = EXCLUDED.volume,
                capacity = EXCLUDED.capacity,
                packaged_volume = EXCLUDED.packaged_volume,
                mass = EXCLUDED.mass,
                portion_size = EXCLUDED.portion_size,
                radius = EXCLUDED.radius,
                published = EXCLUDED.published,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, types, cancellationToken: cancellationToken));
    }

    public async Task<ItemType?> GetTypeAsync(int typeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM types WHERE type_id = @TypeId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ItemType>(new CommandDefinition(sql, new { TypeId = typeId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetTypesCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM types";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<List<int>> GetAllTypeIdsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT type_id FROM types";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        var result = await connection.QueryAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result.ToList();
    }

    #endregion

    #region Regions

    public async Task<int> BulkInsertRegionsAsync(IEnumerable<Region> regions, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO regions (region_id, name, description, created_at, updated_at)
            VALUES (@RegionId, @Name, @Description, @CreatedAt, @UpdatedAt)
            ON CONFLICT (region_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, regions, cancellationToken: cancellationToken));
    }

    public async Task<Region?> GetRegionAsync(int regionId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM regions WHERE region_id = @RegionId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Region>(new CommandDefinition(sql, new { RegionId = regionId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetRegionsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM regions";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion

    #region Constellations

    public async Task<int> BulkInsertConstellationsAsync(IEnumerable<Constellation> constellations, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO constellations (constellation_id, name, region_id, position_x, position_y, position_z, created_at, updated_at)
            VALUES (@ConstellationId, @Name, @RegionId, @PositionX, @PositionY, @PositionZ, @CreatedAt, @UpdatedAt)
            ON CONFLICT (constellation_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                region_id = EXCLUDED.region_id,
                position_x = EXCLUDED.position_x,
                position_y = EXCLUDED.position_y,
                position_z = EXCLUDED.position_z,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, constellations, cancellationToken: cancellationToken));
    }

    public async Task<Constellation?> GetConstellationAsync(int constellationId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM constellations WHERE constellation_id = @ConstellationId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Constellation>(new CommandDefinition(sql, new { ConstellationId = constellationId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetConstellationsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM constellations";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion

    #region Systems

    public async Task<int> BulkInsertSystemsAsync(IEnumerable<SolarSystem> systems, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO systems (system_id, name, constellation_id, position_x, position_y, position_z,
                               security_status, security_class, star_id, created_at, updated_at)
            VALUES (@SystemId, @Name, @ConstellationId, @PositionX, @PositionY, @PositionZ,
                    @SecurityStatus, @SecurityClass, @StarId, @CreatedAt, @UpdatedAt)
            ON CONFLICT (system_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                constellation_id = EXCLUDED.constellation_id,
                position_x = EXCLUDED.position_x,
                position_y = EXCLUDED.position_y,
                position_z = EXCLUDED.position_z,
                security_status = EXCLUDED.security_status,
                security_class = EXCLUDED.security_class,
                star_id = EXCLUDED.star_id,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, systems, cancellationToken: cancellationToken));
    }

    public async Task<SolarSystem?> GetSystemAsync(int systemId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM systems WHERE system_id = @SystemId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SolarSystem>(new CommandDefinition(sql, new { SystemId = systemId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetSystemsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM systems";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion

    #region Stations

    public async Task<int> BulkInsertStationsAsync(IEnumerable<Station> stations, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO stations (station_id, name, system_id, type_id, owner, position_x, position_y, position_z,
                                race_id, services, created_at, updated_at)
            VALUES (@StationId, @Name, @SystemId, @TypeId, @Owner, @PositionX, @PositionY, @PositionZ,
                    @RaceId, @Services, @CreatedAt, @UpdatedAt)
            ON CONFLICT (station_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                system_id = EXCLUDED.system_id,
                type_id = EXCLUDED.type_id,
                owner = EXCLUDED.owner,
                position_x = EXCLUDED.position_x,
                position_y = EXCLUDED.position_y,
                position_z = EXCLUDED.position_z,
                race_id = EXCLUDED.race_id,
                services = EXCLUDED.services,
                updated_at = EXCLUDED.updated_at";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, stations, cancellationToken: cancellationToken));
    }

    public async Task<Station?> GetStationAsync(int stationId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM stations WHERE station_id = @StationId";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Station>(new CommandDefinition(sql, new { StationId = stationId }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetStationsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM stations";

        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    #endregion
}
