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
    public class Startup
    {
        public Startup(IConfiguration configuration)
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
            
            services
                .AddHealthChecks()
                .AddApplicationInsightsPublisher(key)
                .AddPrometheusGatewayPublisher("http://pushgateway:9091/metrics", "pushgateway")
                .AddCheck<CircuitBreakerHealthCheck>("circuitbreakers")
                .AddCheck<ForcedHealthCheck>("forceable")
                .AddCheck<SlowDependencyHealthCheck>("slow", tags: new string[] { "ready" })
                .AddCheck<TripwireHealthCheck>("tripwire", failureStatus: HealthStatus.Degraded);

            services.Configure<CircuitBreakerHealthCheckOptions>(Configuration.GetSection("CircuitBreakerHealthCheckOptions"));
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);

            services.Configure<HealthCheckPublisherOptions>(options => {
                options.Delay = TimeSpan.FromSeconds(20);
                });

            // Registering health check lifetimes. Singleton is preferred
            services.AddSingleton<TripwireHealthCheck>();
            services.AddSingleton(new ForcedHealthCheck(Configuration["HEALTH_INITIAL_STATE"]));

            services.AddSingleton<SlowDependencyHealthCheck>();
            services.AddSingleton(new SqlConnectionHealthCheck(
                new SqlConnection(Configuration.GetConnectionString("Test"))));

            // Register dependencies of health checks
            services.AddSingleton<IRandomHealthCheckResultGenerator, TimeBasedRandomHealthCheckResultGenerator>();
            services.AddSingleton<RandomHealthCheck>();

            services.AddHealthChecksUI();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/ping", new HealthCheckOptions() { Predicate = _ => false });

            HealthCheckOptions options = new HealthCheckOptions();
            options.ResultStatusCodes[HealthStatus.Degraded] = 418; // I'm a tea pot (or other HttpStatusCode enum)
            options.AllowCachingResponses = true;
            options.Predicate = _ => true;
            options.ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse;
            app.UseHealthChecks("/health", options);

            app.UseHealthChecksUI();
            //app.UseHealthChecksUI(setup =>
            //{
            //    setup.UIPath = "/show-health-ui"; // this is ui path in your browser
            //    setup.ApiPath = "/health-ui-api"; // the UI ( spa app )  use this path to get information from the store ( this is NOT the healthz path, is internal ui api )
            //});
            app.UseHttpsRedirection();
            app.UseMvc();
        }

        public void ConfigureProduction(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecks("/health/ready", 80,
                new HealthCheckOptions()
                {
                    Predicate = reg => reg.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            app.UseHealthChecks("/health/lively", 80,
                new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            //app.UseHttpsRedirection();
            app.UseWhen(
                ctx => ctx.User.Identity.IsAuthenticated,
                a => a.UseHealthChecks("/securehealth", new HealthCheckOptions() { Predicate = _ => false })
            );
            app.UseMvc();
        }
    }
}
