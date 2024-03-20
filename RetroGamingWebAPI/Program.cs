using System;
using System.Data.SqlClient;
using System.Net;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using RetroGamingWebAPI.HealthChecks;

var builder = WebApplication.CreateBuilder();

var section = builder.Configuration.GetSection("KeyVault");
Uri keyVaultUri = new Uri(section["KeyVaultName"]);
ClientSecretCredential credential = new ClientSecretCredential(
    section["TenantId"],
    section["ClientId"],
    section["ClientSecret"]);
// For managed identities use: new DefaultAzureCredential()

var secretClient = new SecretClient(keyVaultUri, credential);
builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

// Registering health check lifetimes. Singleton is preferred
builder.Services.AddSingleton<TripwireHealthCheck>();
builder.Services.AddSingleton(new ForcedHealthCheck(builder.Configuration["HEALTH_INITIAL_STATE"]));

builder.Services.AddSingleton<SlowDependencyHealthCheck>();
builder.Services.AddSingleton(new SqlConnectionHealthCheck(
    new SqlConnection(builder.Configuration.GetConnectionString("Test"))));

// Register dependencies of health checks
builder.Services.AddSingleton<IRandomHealthCheckResultGenerator, TimeBasedRandomHealthCheckResultGenerator>();
builder.Services.AddSingleton<RandomHealthCheck>();

IHealthChecksBuilder healthChecks = builder.Services.AddHealthChecks();

healthChecks
//    .AddApplicationInsightsPublisher(builder.Configuration["ApplicationInsights:ConnectionString"])
    .AddSeqPublisher(options => options.Endpoint = builder.Configuration["Seq:Endpoint"])

    .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 50)

    .AddCheck<ForcedHealthCheck>("forceable")
    .AddCheck<SlowDependencyHealthCheck>("slow", tags: new string[] { "ready" })
    .AddCheck<TripwireHealthCheck>("tripwire", failureStatus: HealthStatus.Degraded)

    .AddAzureKeyVault(keyVaultUri, credential,
        options => {
            options
            .AddSecret("ApplicationInsights--InstrumentationKey")
            .AddKey("RetroKey");
        }, name: "keyvault"
    );

builder.Services
    .AddHealthChecksUI()
    .AddInMemoryStorage();

builder.Services.AddMvc().AddNewtonsoftJson();

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseHealthChecksPrometheusExporter(
    "/healthmetrics",
    options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK
);


if (app.Environment.EnvironmentName == Environments.Development)
{
    app.MapHealthChecks("/ping", new HealthCheckOptions() { Predicate = _ => false });
    app.MapHealthChecks("/pong", new HealthCheckOptions() { Predicate = _ => true });
    app.UseDeveloperExceptionPage();

    // Demo purposes: expose endpoint without authentication or authorization
    app.MapHealthChecks("/health",
        new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

    app.MapHealthChecksUI(setup =>
     {
        setup.AddCustomStylesheet("dotnet.css");
     });
}
else
{
    app.MapHealthChecks("/securehealth", new HealthCheckOptions() { Predicate = _ => false })
       .RequireAuthorization();

    app.MapHealthChecks("/health/ready",
        new HealthCheckOptions()
        {
            Predicate = reg => reg.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
    .RequireHost($"*:{app.Configuration["ManagementPort"]}");

    app.MapHealthChecks("/health/lively",
        new HealthCheckOptions()
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
    .RequireHost($"*:{app.Configuration["ManagementPort"]}");
}

app.MapControllers();

app.Run();
