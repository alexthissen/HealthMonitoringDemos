using System;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RetroGamingWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Assembly startupAssembly = typeof(Startup).GetTypeInfo().Assembly;
            return Host.CreateDefaultBuilder(args).
                ConfigureAppConfiguration(builder => 
                {
                    // Add configuration provider for Azure Key Vault
                    IConfiguration configuration = builder.Build();
                    if (!String.IsNullOrEmpty(configuration["KeyVaultName"]))
                    {
                        Uri keyVaultUri = new Uri(configuration["KeyVaultName"]);
                        ClientSecretCredential credential = new ClientSecretCredential(
                            configuration["KeyVaultTenantID"],
                            configuration["KeyVaultClientID"],
                            configuration["KeyVaultClientSecret"]);
                        // For managed identities use: new DefaultAzureCredential()
                        
                        var secretClient = new SecretClient(keyVaultUri, credential);
                        builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup(startupAssembly.GetName().Name);
                });
        }
    }
}
