using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using RetroGamingWebAPI.HealthChecks;

namespace RetroGamingWebAPI
{
    public class StartupProduction
    {
        public StartupProduction(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string key = Configuration["ApplicationInsights:InstrumentationKey"];

            CircuitBreakerPolicy breaker = Policy
                .Handle<HttpRequestException>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromMinutes(1)
            );
            PolicyRegistry registry = new PolicyRegistry() {
                { "DefaultBreaker", breaker }
            };

            //services.AddHealthChecksUI();
            services
                .AddHealthChecks()
                .AddApplicationInsightsPublisher(key)
                .AddPrometheusGatewayPublisher("http://pushgateway:9091/metrics", "pushgateway")
                //.AddCheck<CircuitBreakerHealthCheck>("circuitbreakers", tags: new string[] { "ready" });
                //                .AddAsyncCheck("random", async () => await Task.FromResult(new Random().Next(1000) > 500 ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy()))
                //.AddCheck<ForcedHealthCheck>("forceable")
                .AddCheck<TripwireHealthCheck>("tripwire", failureStatus: HealthStatus.Degraded);

            services.Configure<CircuitBreakerHealthCheckOptions>(Configuration.GetSection("CircuitBreakerHealthCheckOptions"));
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

            services.Configure<HealthCheckPublisherOptions>(options => {
                options.Delay = TimeSpan.FromSeconds(20);
            });

            // Registering health check lifetimes. Singleton is preferred
            services.AddSingleton<TripwireHealthCheck>();
            services.AddSingleton(new ForcedHealthCheck(Configuration["HEALTH_INITIAL_STATE"]));

            services.AddSingleton<IHealthCheck>(new SqlConnectionHealthCheck(
                new SqlConnection(Configuration.GetConnectionString("Test"))));

            // Register dependencies of health checks
            services.AddSingleton<IRandomHealthCheckResultGenerator, TimeBasedRandomHealthCheckResultGenerator>();
            services.AddSingleton<IHealthCheck, RandomHealthCheck>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecks("/health/ready", 8080,
                new HealthCheckOptions()
                {
                    Predicate = reg => reg.Tags.Contains("ready")
                });
            app.UseHealthChecks("/health/lively", 8080,
                new HealthCheckOptions()
                {
                    Predicate = _ => true
                });

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
