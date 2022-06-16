using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    public class RandomHealthCheck : IHealthCheck
    {
        private readonly IRandomHealthCheckResultGenerator generator;

        public RandomHealthCheck(IRandomHealthCheckResultGenerator generator)
        {
            this.generator = generator;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(generator.GenerateRandomResult());
        }
    }

    public interface IRandomHealthCheckResultGenerator
    {
        HealthCheckResult GenerateRandomResult();
    }

    public class TimeBasedRandomHealthCheckResultGenerator : IRandomHealthCheckResultGenerator
    {
        public HealthCheckResult GenerateRandomResult()
        {
            if (DateTime.UtcNow.Minute % 2 == 0)
            {
                return HealthCheckResult.Healthy();
            }

            return HealthCheckResult.Unhealthy(description: "failed");
        }
    }

    public class TotallyRandomHealthCheckResultGenerator : IRandomHealthCheckResultGenerator
    {
        public HealthCheckResult GenerateRandomResult()
        {
            int value = RandomNumberGenerator.GetInt32(100);
            switch (value)
            {
                case int n when (n >= 80):
                    return HealthCheckResult.Unhealthy();
                case int n when (n >= 50):
                    return HealthCheckResult.Degraded();
                default:
                    return HealthCheckResult.Healthy();
            }
        }
    }
}
