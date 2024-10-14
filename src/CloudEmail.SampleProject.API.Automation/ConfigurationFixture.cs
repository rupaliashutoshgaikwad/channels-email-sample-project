using Microsoft.Extensions.Configuration;
using System;

namespace CloudEmail.SampleProject.API.Automation
{
    public class ConfigurationFixture : IDisposable
    {
        private const string DefaultEnvironment = "Development";

        public IConfiguration Configuration { get; }

        public ConfigurationFixture()
        {
            SetTestEnvironment();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            Configuration = new ConfigurationBuilder()
                .AddYamlFile($"AppSettings.{configuration[TestConstants.AspNetCoreEnvironmentName]}.yml", false, true)
                .Build();
        }

        public void Dispose()
        {
            // Cleanup
        }

        private static void SetTestEnvironment()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TestConstants.AspNetCoreEnvironmentName)))
            {
                return;
            }

            var environmentName = new ConfigurationBuilder().AddJsonFile(TestConstants.LaunchSettingsPath).Build()[TestConstants.LaunchSettingsAspNetCoreEnvName];

            environmentName = string.IsNullOrEmpty(environmentName) ? DefaultEnvironment : environmentName;

            Environment.SetEnvironmentVariable(TestConstants.AspNetCoreEnvironmentName, environmentName);
        }
    }
}
