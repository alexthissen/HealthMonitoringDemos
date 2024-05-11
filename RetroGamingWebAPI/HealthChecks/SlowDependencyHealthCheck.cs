using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace RetroGamingWebAPI.HealthChecks
{
    // From https://github.com/aspnet/Diagnostics/blob/master/samples/HealthChecksSample/SlowDependencyHealthCheck.cs
    public class SlowDependencyHealthCheck : IHealthCheck
    {
        public static readonly string HealthCheckName = "slow_dependency";

        private readonly Task _task;

        public SlowDependencyHealthCheck()
        {
            _task = Task.Delay(5 * 1000);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_task.IsCompleted)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Dependency is ready"));
            }

            return Task.FromResult(new HealthCheckResult(
                status: context.Registration.FailureStatus,
                description: "Dependency is still initializing"));
        }
    }
}
