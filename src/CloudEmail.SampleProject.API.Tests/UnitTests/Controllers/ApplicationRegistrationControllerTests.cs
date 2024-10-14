using CloudEmail.ApiAuthentication.Models;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Controllers;
using CloudEmail.SampleProject.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Controllers
{
    [ExcludeFromCodeCoverage]
    public class ApplicationRegistrationControllerTests
    {
        [Theory]
        [AutoMoqData]
        public async Task ApplicationRegistrationController_GivenNewRegistration_SavedInDb(
            ApplicationRegistration registration)
        {
            // ARRANGE
            using (var ctx = GetWriteApiDbContext("ApplicationRegistrationController_GivenNewRegistration_SavedInDb"))
            {
                await ctx.Database.EnsureCreatedAsync();
            }
            var controller = new ApplicationRegistrationController(GetReadApiDbContext("ApplicationRegistrationController_GivenNewRegistration_SavedInDb"), GetWriteApiDbContext("ApplicationRegistrationController_GivenNewRegistration_SavedInDb"));

            // ACT
            var result = await controller.PostApplicationRegistration(registration);

            // ASSERT
            Assert.Equal(typeof(CreatedAtActionResult), result.GetType());
        }

        [Theory]
        [AutoMoqData]
        public async Task ApplicationRegistrationController_GivenExistingRegistration_ReturnsConflict(
            ApplicationRegistration registration)
        {
            // ARRANGE
            using (var ctx = GetReadApiDbContext("ApplicationRegistrationController_GivenExistingRegistration_ReturnsConflict"))
            {
                await ctx.Database.EnsureCreatedAsync();
                await ctx.ApplicationRegistrations.AddAsync(registration);
                await ctx.SaveChangesAsync();
            }
            var controller = new ApplicationRegistrationController(GetReadApiDbContext("ApplicationRegistrationController_GivenExistingRegistration_ReturnsConflict"),
                GetWriteApiDbContext("ApplicationRegistrationController_GivenExistingRegistration_ReturnsConflict"));

            // ACT
            var result = await controller.PostApplicationRegistration(registration);

            // ASSERT
            Assert.Equal(typeof(ConflictResult), result.GetType());
        }

        [Theory]
        [AutoMoqData]
        public async Task ApplicationRegistrationController_ReturnAllRegistrations(
            ApplicationRegistration registration)
        {
            // ARRANGE
            var configuration = new ConfigurationBuilder().AddYamlFile("AppSettings.UnitTest.yml").Build();

            using (var ctx = GetWriteApiDbContext("ApplicationRegistrationController_ReturnAllRegistrations"))
            {
                await ctx.Database.EnsureCreatedAsync();
                await ctx.ApplicationRegistrations.AddAsync(registration);
                await ctx.SaveChangesAsync();
            }
            var controller = new ApplicationRegistrationController(GetReadApiDbContext("ApplicationRegistrationController_ReturnAllRegistrations"), 
                GetWriteApiDbContext("ApplicationRegistrationController_ReturnAllRegistrations"));

            // ACT
            var result = controller.GetApplicationRegistrations();

            // ASSERT
            Assert.All(result, s => s.Token = registration.Token);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetApplicationRegistration_GivenValidId_ReturnRegistration(
            int id)
        {
            // ARRANGE
            var readContext = GetReadApiDbContext("GetApplicationRegistration_GivenValidId_ReturnRegistration", id);
            var writeContext = GetWriteApiDbContext("GetApplicationRegistration_GivenValidId_ReturnRegistration");

            var controller = new ApplicationRegistrationController(readContext, writeContext);

            // ACT
            var result = await controller.GetApplicationRegistration(id);

            // ASSERT
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            var resultModel = okResult.Value as ApplicationRegistration;
            Assert.NotNull(resultModel);
            Assert.Equal(id, resultModel.Id);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetApplicationRegistration_GivenInValidId_ReturnNotFound(
            int id)
        {
            // ARRANGE
            var readContext = GetReadApiDbContext("GetApplicationRegistration_GivenInValidId_ReturnNotFound");
            var writeContext = GetWriteApiDbContext("GetApplicationRegistration_GivenInValidId_ReturnNotFound");

            var controller = new ApplicationRegistrationController(readContext, writeContext);

            // ACT
            var result = await controller.GetApplicationRegistration(id);

            // ASSERT
            var notFoundResult = result as NotFoundResult;
            Assert.NotNull(notFoundResult);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task DeleteApplicationRegistration_GivenValidId_ReturnRegistration(
            int id)
        {
            // ARRANGE
            var readContext = GetReadApiDbContext("DeleteApplicationRegistration_GivenValidId_ReturnRegistration");
            var writeContext = GetWriteApiDbContext("DeleteApplicationRegistration_GivenValidId_ReturnRegistration", id);

            var controller = new ApplicationRegistrationController(readContext, writeContext);

            // ACT
            var result = await controller.DeleteApplicationRegistration(id);

            // ASSERT
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            var resultModel = okResult.Value as ApplicationRegistration;
            Assert.NotNull(resultModel);
            Assert.Equal(id, resultModel.Id);
        }

        [Theory]
        [AutoMoqData]
        public async Task DeleteApplicationRegistration_GivenInValidId_ReturnNotFound(
            int id)
        {
            // ARRANGE
            var readContext = GetReadApiDbContext("DeleteApplicationRegistration_GivenInValidId_ReturnNotFound");
            var writeContext = GetWriteApiDbContext("DeleteApplicationRegistration_GivenInValidId_ReturnNotFound");

            var controller = new ApplicationRegistrationController(readContext, writeContext);

            // ACT
            var result = await controller.DeleteApplicationRegistration(id);

            // ASSERT
            var notFoundResult = result as NotFoundResult;
            Assert.NotNull(notFoundResult);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        private static ReadApiDbContext GetReadApiDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ReadApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;
            return new ReadApiDbContext(options);
        }

        private static WriteApiDbContext GetWriteApiDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<WriteApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;
            return new WriteApiDbContext(options);
        }

        private static WriteApiDbContext GetWriteApiDbContext(string dbName, int id)
        {
            var options = new DbContextOptionsBuilder<WriteApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;

            var context = new WriteApiDbContext(options);

            context.ApplicationRegistrations.Add(new ApplicationRegistration
            {
                Id = id,
                Created = DateTime.Now,
                Name = "Test",
                Token = "TestToken"
            });

            context.SaveChanges();

            return context;
        }

        private static ReadApiDbContext GetReadApiDbContext(string dbName, int id)
        {
            var options = new DbContextOptionsBuilder<ReadApiDbContext>()
                .UseInMemoryDatabase(dbName, d => d.EnableNullChecks(false))
                .Options;

            var context = new ReadApiDbContext(options);

            context.ApplicationRegistrations.Add(new ApplicationRegistration
            {
                Id = id,
                Created = DateTime.Now,
                Name = "Test",
                Token = "TestToken"
            });

            context.SaveChanges();

            return context;
        }
    }
}
