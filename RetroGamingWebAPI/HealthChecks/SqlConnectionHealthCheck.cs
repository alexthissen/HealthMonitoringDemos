using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    public class SqlConnectionHealthCheck : IHealthCheck
    {
        SqlConnection connection;

        public string Name => "sql";

        public SqlConnectionHealthCheck(SqlConnection connection)
        {
            this.connection = connection;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await connection.OpenAsync();
            }
            catch (SqlException)
            {
                return HealthCheckResult.Unhealthy();
            }

            return HealthCheckResult.Healthy();
        }
    }
}
