using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class ManualLogService
    {
        private readonly string _connectionString;

        public ManualLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task LogMoveAsync(string treeName, string fromLocation, string toLocation, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO dbo.TreeTrackerManualLog 
                    (ActionType, TreeName, FromLocation, ToLocation, UserID, Notes)
                  VALUES 
                    ('MOVE', @TreeName, @FromLocation, @ToLocation, @UserID, 
                     'Tree manually moved from ' + @FromLocation + ' to ' + @ToLocation)",
                new { TreeName = treeName, FromLocation = fromLocation, ToLocation = toLocation, UserID = userId }
            );
        }

        public async Task LogAddAsync(string shopOrderNo, string treeName, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO dbo.TreeTrackerManualLog 
                    (ActionType, ShopOrderNo, TreeName, UserID, Notes)
                  VALUES 
                    ('ADD', @ShopOrderNo, @TreeName, @UserID,
                     'Shop order manually added to tree ' + @TreeName)",
                new { ShopOrderNo = shopOrderNo, TreeName = treeName, UserID = userId }
            );
        }

        public async Task<IEnumerable<ManualLog>> GetAllLogsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ManualLog>(
                "SELECT * FROM dbo.TreeTrackerManualLog ORDER BY ActionAt DESC"
            );
        }
    }
}