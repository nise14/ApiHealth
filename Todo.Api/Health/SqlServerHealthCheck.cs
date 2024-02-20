using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Todo.Api.Health;

public class SqlServerHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public SqlServerHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("SELECT 1;", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, description: "Database is unhealthy", exception: ex);
        }
    }
}