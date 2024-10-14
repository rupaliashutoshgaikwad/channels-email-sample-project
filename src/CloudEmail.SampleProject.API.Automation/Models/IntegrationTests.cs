using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CloudEmail.SampleProject.API.Automation.Models
{
    [Trait("Category", "Integration")]
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>> { }
}