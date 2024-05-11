using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    public class TripwireHealthCheck : IHealthCheck
    {
        private static int trippedCount = 0;

        public int Trip()
        {
            return Interlocked.Increment(ref trippedCount);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (trippedCount % 3)
            {
                case 2:
                    return Task.FromResult(HealthCheckResult.Unhealthy("Boom!"));
                case 1:
                    return Task.FromResult(HealthCheckResult.Degraded("About to explode"));
                default:
                    return Task.FromResult(HealthCheckResult.Healthy("Still doing okay"));
            };
        }
    }
}
