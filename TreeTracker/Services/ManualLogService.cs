using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class ManualLogService
    {
        private readonly string _connectionString;
        private readonly ILogger<ManualLogService> _logger;

        public ManualLogService(IConfiguration configuration, ILogger<ManualLogService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<ServiceResult> LogMoveAsync(string treeName, string fromLocation, string toLocation, string userId)
        {
            try
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
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging move for tree {TreeName}", treeName);
                return ServiceResult.Fail("Move completed but could not be logged.");
            }
        }

        public async Task<ServiceResult> LogAddAsync(string shopOrderNo, string treeName, string userId)
        {
            try
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
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging add for shop order {ShopOrderNo}", shopOrderNo);
                return ServiceResult.Fail("Add completed but could not be logged.");
            }
        }

        public async Task<ServiceResult<IEnumerable<ManualLog>>> GetAllLogsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<ManualLog>(
                    "SELECT * FROM dbo.TreeTrackerManualLog ORDER BY ActionAt DESC"
                );
                return ServiceResult<IEnumerable<ManualLog>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching manual logs");
                return ServiceResult<IEnumerable<ManualLog>>.Fail("Unable to load manual action log.");
            }
        }
    }
}