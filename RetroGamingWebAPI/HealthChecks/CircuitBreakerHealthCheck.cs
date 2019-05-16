using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    public class CircuitBreakerHealthCheckOptions
    {
        public List<string> CircuitBreakerNames { get; set; }
    }

    public class CircuitBreakerHealthCheck : IHealthCheck
    {
        private readonly IOptions<CircuitBreakerHealthCheckOptions> options;
        private readonly IReadOnlyPolicyRegistry<string> registry;

        public CircuitBreakerHealthCheck(IOptions<CircuitBreakerHealthCheckOptions> options, IReadOnlyPolicyRegistry<string> registry)
        {
            this.options = options;
            this.registry = registry;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            CircuitBreakerPolicy policy;
            HealthCheckResult result = HealthCheckResult.Healthy();
            foreach (string name in options.Value.CircuitBreakerNames)
            {
                if (registry.TryGet<CircuitBreakerPolicy>(name, out policy))
                {
                    if (policy.CircuitState == CircuitState.Isolated || policy.CircuitState == CircuitState.HalfOpen)
                    {
                        result = HealthCheckResult.Degraded(description: "Too many circuit breakers are (half)open.");
                    }
                }
            }
            return Task.FromResult(result);
        }
    }
}
