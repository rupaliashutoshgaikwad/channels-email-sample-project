using CloudEmail.SampleProject.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class ServiceCollectionExtensionTests
    {
        [Fact]
        public void ConfigureSmtpServiceConfiguration_GivenValidKerioHost_SetSuccessfully()
        {
            // ARRANGE
            var configurationList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.KerioHost, "testHost")
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationList)
                .Build();

            var serviceCollection = new ServiceCollection();

            // ACT
            serviceCollection.Configure<SmtpServiceConfiguration>(o => o.KerioHost = configuration[Constants.KerioHost]);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // ASSERT

            var options = serviceProvider.GetRequiredService<IOptions<SmtpServiceConfiguration>>();
            Assert.Equal("testHost", options.Value.KerioHost);
        }
    }
}
