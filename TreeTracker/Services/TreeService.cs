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
    }
}