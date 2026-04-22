using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class TreeService
    {
        private readonly string _connectionString;
        private readonly ILogger<TreeService> _logger;

        public static readonly List<string> Locations = new()
        {
            "Conductor Line",
            "Coating Start",
            "Coating Finish",
            "Plating Start",
            "Plating Finish",
            "Final Assembly"
        };

        public TreeService(IConfiguration configuration, ILogger<TreeService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<ServiceResult<IEnumerable<string>>> GetTreeNamesByLocationAsync(string location)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<string>(
                    "SELECT DISTINCT CurrentTree FROM dbo.TreeTracker WHERE TreeLocation = @Location ORDER BY CurrentTree",
                    new { Location = location }
                );
                return ServiceResult<IEnumerable<string>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tree names for location {Location}", location);
                return ServiceResult<IEnumerable<string>>.Fail("Unable to load trees. Please try again.");
            }
        }

        public async Task<ServiceResult<IEnumerable<TreeTrackerItem>>> GetPartsByTreeAsync(string treeName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<TreeTrackerItem>(
                    "SELECT * FROM dbo.TreeTracker WHERE CurrentTree = @TreeName ORDER BY TimeAdded DESC",
                    new { TreeName = treeName }
                );
                return ServiceResult<IEnumerable<TreeTrackerItem>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching parts for tree {TreeName}", treeName);
                return ServiceResult<IEnumerable<TreeTrackerItem>>.Fail("Unable to load parts. Please try again.");
            }
        }

        public async Task<ServiceResult<TreeTrackerItem?>> GetTreeByShopOrderAsync(string shopOrderNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                    "SELECT * FROM dbo.TreeTracker WHERE ShopOrderNo = @ShopOrderNo",
                    new { ShopOrderNo = shopOrderNo }
                );
                return ServiceResult<TreeTrackerItem?>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tree for shop order {ShopOrderNo}", shopOrderNo);
                return ServiceResult<TreeTrackerItem?>.Fail("Unable to search for shop order. Please try again.");
            }
        }

        public async Task<ServiceResult<IEnumerable<LocationSummary>>> GetLocationSummaryAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var results = new List<LocationSummary>();

                foreach (var location in Locations)
                {
                    var summary = await connection.QueryFirstOrDefaultAsync<LocationSummary>(
                        @"SELECT @Location AS Location,
                            COUNT(DISTINCT CurrentTree) AS TreeCount,
                            COUNT(ShopOrderNo) AS ShopOrderCount
                          FROM dbo.TreeTracker
                          WHERE TreeLocation = @Location",
                        new { Location = location }
                    );
                    results.Add(summary ?? new LocationSummary { Location = location });
                }

                return ServiceResult<IEnumerable<LocationSummary>>.Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching location summary");
                return ServiceResult<IEnumerable<LocationSummary>>.Fail("Unable to load dashboard data. Please try again.");
            }
        }

        public async Task<ServiceResult<IEnumerable<string>>> GetAllTreeNamesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<string>(
                    "SELECT DISTINCT CurrentTree FROM dbo.TreeTracker ORDER BY CurrentTree"
                );
                return ServiceResult<IEnumerable<string>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all tree names");
                return ServiceResult<IEnumerable<string>>.Fail("Unable to load tree list. Please try again.");
            }
        }

        public async Task<ServiceResult<IEnumerable<EngravingPart>>> GetEngravingPartsAsync(string shopOrderNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<EngravingPart>(
                    @"WITH LatestBatch AS (
                        SELECT MAX(CAST(CreatedAt AS DATE)) AS LatestDate
                        FROM [ERPPBG].[prod].[tbl_EngravingLog]
                        WHERE ShopOrderNo = @ShopOrderNo
                      )
                      SELECT DISTINCT e.Phase, e.SetID, e.SubPartID
                      FROM [ERPPBG].[prod].[tbl_EngravingLog] e
                      INNER JOIN LatestBatch b ON CAST(e.CreatedAt AS DATE) = b.LatestDate
                      WHERE e.ShopOrderNo = @ShopOrderNo
                      ORDER BY e.Phase, e.SetID",
                    new { ShopOrderNo = shopOrderNo }
                );
                return ServiceResult<IEnumerable<EngravingPart>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching engraving parts for shop order {ShopOrderNo}", shopOrderNo);
                return ServiceResult<IEnumerable<EngravingPart>>.Fail("Unable to load parts from Engraving Log. Please try again.");
            }
        }

        public async Task<ServiceResult<TreeTrackerItem?>> GetMasterRecordAsync(string shopOrderNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                    "SELECT * FROM prod.Master WHERE ShopOrderNo = @ShopOrderNo",
                    new { ShopOrderNo = shopOrderNo }
                );
                return ServiceResult<TreeTrackerItem?>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching master record for shop order {ShopOrderNo}", shopOrderNo);
                return ServiceResult<TreeTrackerItem?>.Fail("Unable to check prod.Master. Please try again.");
            }
        }

        public async Task<ServiceResult> MoveTreeAsync(string treeName, string newLocation, string previousTree)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    @"UPDATE dbo.TreeTracker 
                      SET TreeLocation = @NewLocation, PreviousTree = @PreviousTree
                      WHERE CurrentTree = @TreeName",
                    new { TreeName = treeName, NewLocation = newLocation, PreviousTree = previousTree }
                );
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving tree {TreeName} to {NewLocation}", treeName, newLocation);
                return ServiceResult.Fail("Unable to move tree. Please try again.");
            }
        }

        public async Task<ServiceResult<bool>> ManuallyAddShopOrderAsync(string shopOrderNo, string treeName, string addedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var exists = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(1) FROM dbo.TreeTracker WHERE ShopOrderNo = @ShopOrderNo",
                    new { ShopOrderNo = shopOrderNo }
                );

                if (exists > 0)
                    return ServiceResult<bool>.Fail($"Shop order {shopOrderNo} already exists in TreeTracker.");

                var master = await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                    "SELECT * FROM prod.Master WHERE ShopOrderNo = @ShopOrderNo",
                    new { ShopOrderNo = shopOrderNo }
                );

                if (master == null)
                    return ServiceResult<bool>.Fail($"Shop order {shopOrderNo} was not found in prod.Master.");

                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.TreeTracker 
                        (ProjectID, ShopOrderNo, WorkOrderNo, TagNo, PartID, CurrentTree, TimeAdded, TreeLocation, PreviousTree)
                      SELECT ProjectID, ShopOrderNo, JobID, Tag_No, PartID, @TreeName, GETDATE(), NULL, NULL
                      FROM prod.Master
                      WHERE ShopOrderNo = @ShopOrderNo",
                    new { ShopOrderNo = shopOrderNo, TreeName = treeName }
                );

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually adding shop order {ShopOrderNo} to tree {TreeName}", shopOrderNo, treeName);
                return ServiceResult<bool>.Fail("Unable to add shop order. Please try again.");
            }
        }
    }
}