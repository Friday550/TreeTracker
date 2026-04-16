// Services/TreeService.cs
using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class TreeService
    {
        private readonly string _connectionString;

        // Define the fixed locations
        public static readonly List<string> Locations = new()
        {
            "Conductor Line",
            "Coating Start",
            "Coating Finish",
            "Plating Start",
            "Plating Finish",
            "Final Assembly"
        };

        public TreeService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // Get distinct tree names for a specific location
        public async Task<IEnumerable<string>> GetTreeNamesByLocationAsync(string location)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<string>(
                "SELECT DISTINCT CurrentTree FROM ERPPBG.dbo.TreeTracker WHERE TreeLocation = @Location ORDER BY CurrentTree",
                new { Location = location }
            );
        }

        // Get all parts on a specific tree for the modal
        public async Task<IEnumerable<TreeTrackerItem>> GetPartsByTreeAsync(string treeName)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<TreeTrackerItem>(
                "SELECT * FROM ERPPBG.dbo.TreeTracker WHERE CurrentTree = @TreeName ORDER BY TimeAdded DESC",
                new { TreeName = treeName }
            );
        }

        public async Task<TreeTrackerItem?> GetTreeByShopOrderAsync(string shopOrderNo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                "SELECT * FROM ERPPBG.dbo.TreeTracker WHERE ShopOrderNo = @ShopOrderNo",
                new { ShopOrderNo = shopOrderNo }
            );
        }
        public async Task<IEnumerable<LocationSummary>> GetLocationSummaryAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var results = new List<LocationSummary>();

            foreach (var location in Locations)
            {
                var summary = await connection.QueryFirstOrDefaultAsync<LocationSummary>(
                    @"SELECT 
                @Location AS Location,
                COUNT(DISTINCT CurrentTree) AS TreeCount,
                COUNT(ShopOrderNo) AS ShopOrderCount
              FROM dbo.TreeTracker
              WHERE TreeLocation = @Location",
                    new { Location = location }
                );

                results.Add(summary ?? new LocationSummary { Location = location });
            }

            return results;
        }
        public async Task MoveTreeAsync(string treeName, string newLocation, string previousTree)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE dbo.TreeTracker 
          SET TreeLocation = @NewLocation,
              PreviousTree = @PreviousTree
          WHERE CurrentTree = @TreeName",
                new { TreeName = treeName, NewLocation = newLocation, PreviousTree = previousTree }
            );
        }
        // Get child parts from tbl_EngravingLog for a shop order
        public async Task<IEnumerable<EngravingPart>> GetEngravingPartsAsync(string shopOrderNo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<EngravingPart>(
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
        }

        // Get all distinct tree names for the dropdown
        public async Task<IEnumerable<string>> GetAllTreeNamesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<string>(
                "SELECT DISTINCT CurrentTree FROM dbo.TreeTracker ORDER BY CurrentTree"
            );
        }

        // Manually insert a shop order into TreeTracker
        public async Task<bool> ManuallyAddShopOrderAsync(string shopOrderNo, string treeName, string addedBy)
        {
            using var connection = new SqlConnection(_connectionString);

            // Check if already exists
            var exists = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM dbo.TreeTracker WHERE ShopOrderNo = @ShopOrderNo",
                new { ShopOrderNo = shopOrderNo }
            );

            if (exists > 0) return false;

            // Get data from prod.Master
            var master = await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                "SELECT * FROM prod.Master WHERE ShopOrderNo = @ShopOrderNo",
                new { ShopOrderNo = shopOrderNo }
            );

            if (master == null) return false;

            await connection.ExecuteAsync(
                @"INSERT INTO dbo.TreeTracker 
            (ProjectID, ShopOrderNo, WorkOrderNo, TagNo, PartID, CurrentTree, TimeAdded, TreeLocation, PreviousTree)
          SELECT 
            ProjectID, ShopOrderNo, JobID, Tag_No, PartID, @TreeName, GETDATE(), NULL, NULL
          FROM prod.Master
          WHERE ShopOrderNo = @ShopOrderNo",
                new { ShopOrderNo = shopOrderNo, TreeName = treeName }
            );

            return true;
        }

        public async Task<TreeTrackerItem?> GetMasterRecordAsync(string shopOrderNo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TreeTrackerItem>(
                "SELECT * FROM prod.Master WHERE ShopOrderNo = @ShopOrderNo",
                new { ShopOrderNo = shopOrderNo }
            );
        }
    }

}