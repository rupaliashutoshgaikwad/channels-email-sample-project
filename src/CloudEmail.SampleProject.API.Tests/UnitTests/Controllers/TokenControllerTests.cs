using Amazon.Runtime.Internal.Util;
using CloudEmail.API.Models;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.SampleProject.API.Controllers;
using CloudEmail.SampleProject.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Controllers
{
    [ExcludeFromCodeCoverage]
    public class TokenControllerTests
    {
        [Fact]
        public async Task TokenController_GivenExistingRegistration_ReturnToken()
        {
            // ARRANGE
            var configuration = new ConfigurationBuilder().AddYamlFile("AppSettings.UnitTest.yml").Build();

            using (var ctx = GetReadApiDbContext("TokenController_GivenExistingRegistration_ReturnToken"))
            {
                await ctx.Database.EnsureCreatedAsync();
                await ctx.ApplicationRegistrations.AddAsync(new ApplicationRegistration
                {
                    Token = configuration[AuthConstants.ApiIssuerSecret]
                });
                await ctx.SaveChangesAsync();
            }
            var controller = new TokenController(
                configuration,
                GetReadApiDbContext("TokenController_GivenExistingRegistration_ReturnToken"),
                new Mock<ILogger<TokenController>>().Object);

            // ACT
            var result = await controller.Get(new ApiTokenRequest { BasicToken = configuration[AuthConstants.ApiIssuerSecret] });

            // ASSERT
            Assert.Equal(typeof(ApiToken), result.Value.GetType());
        }

        [Fact]
        public async Task TokenController_GivenMissingRegistration_ReturnNoToken()
        {
            // ARRANGE
            var configuration = new ConfigurationBuilder().AddYamlFile("AppSettings.UnitTest.yml").Build();

            using (var ctx = GetWriteApiDbContext("TokenController_GivenMissingRegistration_ReturnNoToken"))
            {
                await ctx.Database.EnsureCreatedAsync();
            }
            var controller = new TokenController(
                configuration,
                GetReadApiDbContext("TokenController_GivenMissingRegistration_ReturnNoToken"),
                new Mock<ILogger<TokenController>>().Object);

            // ACT
            var result = await controller.Get(new ApiTokenRequest { BasicToken = configuration[AuthConstants.ApiIssuerSecret] });

            // ASSERT
            Assert.Equal(typeof(BadRequestObjectResult), result.Result.GetType());
        }

        private ReadApiDbContext GetReadApiDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ReadApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;
            return new ReadApiDbContext(options);
        }

        private WriteApiDbContext GetWriteApiDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<WriteApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;
            return new WriteApiDbContext(options);
        }
    }
}