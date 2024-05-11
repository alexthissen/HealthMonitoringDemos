using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    public class ForcedHealthCheck : IHealthCheck
    {
        private static HealthCheckResult forcedResult = HealthCheckResult.Healthy();

        public ForcedHealthCheck(string initialStatus)
        {
            Force(initialStatus ?? "Healthy", Environment.MachineName);
        }

        public void Force(string status, string node)
        {
            forcedResult = new HealthCheckResult(
                Enum.Parse<HealthStatus>(status, true),
                $"Forced at {DateTime.Now.ToShortTimeString()}",
                null, new Dictionary<string, object>
                    {
                        { "time", DateTime.Now },
                        { "node", node }
                    }
                );
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(forcedResult);
        }
    }
}
