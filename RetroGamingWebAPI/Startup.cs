using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HealthChecks.UI.Client;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
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
            services.Configure<CircuitBreakerHealthCheckOptions>(Configuration.GetSection("CircuitBreakerHealthCheckOptions"));
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);
            
            // Registering health check lifetimes. Singleton is preferred
            services.AddSingleton<TripwireHealthCheck>();
            services.AddSingleton(new ForcedHealthCheck(Configuration["HEALTH_INITIAL_STATE"]));

            services.AddSingleton<SlowDependencyHealthCheck>();
            services.AddSingleton(new SqlConnectionHealthCheck(
                new SqlConnection(Configuration.GetConnectionString("Test"))));

            // Register dependencies of health checks
            services.AddSingleton<IRandomHealthCheckResultGenerator, TimeBasedRandomHealthCheckResultGenerator>();
            services.AddSingleton<RandomHealthCheck>();

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Timeout = TimeSpan.FromSeconds(10);
                //options.Predicate = check => check.Tags.Contains("ready");
            });

            services
                .AddHealthChecks()
                
                .AddApplicationInsightsPublisher(key)
                .AddPrometheusGatewayPublisher("http://pushgateway:9091/metrics", "pushgateway")
                
                .AddCheck<CircuitBreakerHealthCheck>("circuitbreakers")
                .AddCheck<ForcedHealthCheck>("forceable")
                .AddCheck<SlowDependencyHealthCheck>("slow", tags: new string[] { "ready" })
                .AddCheck<TripwireHealthCheck>("tripwire", failureStatus: HealthStatus.Degraded);

            services.Configure<HealthCheckPublisherOptions>(options => {
                options.Delay = TimeSpan.FromSeconds(20);
                });

            services.AddHealthChecksUI();

            services.AddMvc()
                .AddNewtonsoftJson()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHealthChecks("/health", options);
            // app.UseHealthChecksUI();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/ping", new HealthCheckOptions() { Predicate = _ => false });

                HealthCheckOptions options = new HealthCheckOptions();
                options.ResultStatusCodes[HealthStatus.Degraded] = 418; // I'm a tea pot (or other HttpStatusCode enum)
                options.AllowCachingResponses = true;
                options.Predicate = _ => true;
                options.ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse;
                endpoints.MapHealthChecks("/health", options);

                endpoints.MapHealthChecksUI();
                //endpoints.MapHealthChecksUI(setup =>
                //{
                //    setup.UIPath = "/show-health-ui"; // UI path in browser
                //    setup.ApiPath = "/health-ui-api"; // API of SPA application
                //});

                endpoints.MapControllers();
            });
        }

        public void ConfigureProduction(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseHttpsRedirection();
            app.UseWhen(
                ctx => ctx.User.Identity.IsAuthenticated,
                a => a.UseHealthChecks("/securehealth", new HealthCheckOptions() { Predicate = _ => false })
            );
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/ready",
                    new HealthCheckOptions()
                    {
                        Predicate = reg => reg.Tags.Contains("ready"),
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    })
                .RequireHost($"*:{Configuration["ManagementPort"]}");

                endpoints.MapHealthChecks("/health/lively",
                    new HealthCheckOptions()
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    })
                .RequireHost($"*:{Configuration["ManagementPort"]}");

                endpoints.MapControllers();
            });
        }
    }
}