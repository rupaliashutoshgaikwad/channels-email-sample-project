using CloudEmail.SampleProject.API.Data;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using FluentScheduler;

namespace CloudEmail.SampleProject.API
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var db = services.GetRequiredService<WriteApiDbContext>();
                    var migrateDbPolicy = Policy
                        .Handle<Exception>()
                        .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

                    migrateDbPolicy.Execute(() =>
                    {
                        db.Database.Migrate();
                    });

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            int loadCacheTimeHour;
            int loadCacheTimeMinute;
            switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            {
                // Pods are running in UTC
                case "Development":
                case "Virginia":
                case "Montreal":
                // 6 hours behind of UTC
                    loadCacheTimeHour = 5;
                    loadCacheTimeMinute = 30;
                    break;
                case "Frankfurt":
                case "London":
                // 3 hours ahead of UTC 
                    loadCacheTimeHour = 21;
                    loadCacheTimeMinute = 30;
                    break;
                case "Sydney":
                case "Tokyo":
                // 2 hours ahead of UTC
                    loadCacheTimeHour = 14;
                    loadCacheTimeMinute = 30;
                    break;
                default:
                    loadCacheTimeHour = 5;
                    loadCacheTimeMinute = 30;
                    break;
            }

            var cache = host.Services.GetRequiredService<IDomainVerificationService>();

            var registry = new Registry();
            registry.Schedule(() => cache.LoadCache()).ToRunNow().AndEvery(1).Days().At(loadCacheTimeHour, loadCacheTimeMinute);

            JobManager.Initialize(registry);

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    configurationBuilder.AddYamlFile($"AppSettings.{context.HostingEnvironment.EnvironmentName}.yml", false, true);
                    configurationBuilder.AddEnvironmentVariables();
                })
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration))
                .UseStartup<Startup>();
    }
}
