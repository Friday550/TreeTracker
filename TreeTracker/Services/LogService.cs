using Dapper;
using Microsoft.Data.SqlClient;
using TreeTracker.Models;

namespace TreeTracker.Services
{
    public class LogService
    {
        private readonly string _connectionString;
        private readonly ILogger<LogService> _logger;

        public LogService(IConfiguration configuration, ILogger<LogService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<ServiceResult<IEnumerable<TreeTrackerLog>>> GetNewLogsAsync(DateTime since)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<TreeTrackerLog>(
                    "SELECT * FROM dbo.TreeTrackerLog WHERE LoggedAt > @Since ORDER BY LoggedAt DESC",
                    new { Since = since }
                );
                return ServiceResult<IEnumerable<TreeTrackerLog>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching new logs since {Since}", since);
                return ServiceResult<IEnumerable<TreeTrackerLog>>.Fail("Unable to check for new errors.");
            }
        }

        public async Task<ServiceResult<IEnumerable<TreeTrackerLog>>> GetAllLogsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<TreeTrackerLog>(
                    "SELECT * FROM dbo.TreeTrackerLog ORDER BY LoggedAt DESC"
                );
                return ServiceResult<IEnumerable<TreeTrackerLog>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all logs");
                return ServiceResult<IEnumerable<TreeTrackerLog>>.Fail("Unable to load error log.");
            }
        }
    }
}