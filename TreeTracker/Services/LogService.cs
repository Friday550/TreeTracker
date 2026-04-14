using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class LogService
    {
        private readonly string _connectionString;

        public LogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // Get any new log entries since the last check
        public async Task<IEnumerable<TreeTrackerLog>> GetNewLogsAsync(DateTime since)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<TreeTrackerLog>(
                "SELECT * FROM ERPPBG.dbo.TreeTrackerLog WHERE LoggedAt > @Since ORDER BY LoggedAt DESC",
                new { Since = since }
            );
        }

        // Get all logs grouped by RunID for the log page
        public async Task<IEnumerable<TreeTrackerLog>> GetAllLogsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<TreeTrackerLog>(
                "SELECT * FROM ERPPBG.dbo.TreeTrackerLog ORDER BY LoggedAt DESC"
            );
        }
    }
}