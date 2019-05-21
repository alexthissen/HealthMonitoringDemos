using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RetroGamingWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            Assembly startupAssembly = typeof(Startup).GetTypeInfo().Assembly;
            return WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                //.UseStartup<Startup>()
                .UseStartup(startupAssembly.GetName().Name);
                
        }
    }
}
