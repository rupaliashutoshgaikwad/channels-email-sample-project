using CloudEmail.ApiAuthentication;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Automation.Models;
using FluentAssertions;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Automation.Tests.Acceptance.TokenControllerTests
{
    [ExcludeFromCodeCoverage]
    public class GetTests : AcceptanceTests, IClassFixture<ConfigurationFixture>
    {
        private readonly string _apiBaseUrl;
        private readonly string _apiBasicToken;

        public GetTests(ConfigurationFixture configurationFixture)
        {
            _apiBaseUrl = configurationFixture.Configuration["SendEmailApiConfiguration:BaseUrl"];
            _apiBasicToken = configurationFixture.Configuration["SendEmailApiConfiguration:BasicToken"];
        }

        [Theory]
        [InlineData("/Token")]
        public void GivenValidBasicToken_WithoutClient_ReturnsApiToken(string uri)
        {
            // ARRANGE
            var request = new RestRequest(uri, Method.POST);
            var body = new ApiTokenRequest { BasicToken = _apiBasicToken };
            request.AddParameter("text/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);

            var restClient = new RestClient(_apiBaseUrl);

            // ACT
            var response = restClient.Execute(request);

            // ASSERT
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiToken = JsonConvert.DeserializeObject<ApiToken>(response.Content);
            apiToken.Token.Should().NotBeNull();
        }

        [Fact]
        public async Task GivenValidBasicToken_WithClient_ReturnsApiToken()
        {
            // ARRANGE
            var tokenClient = ApiTokenProvider.Create(this._apiBaseUrl, this._apiBasicToken);

            // ACT
            var response = await tokenClient.GetTokenAsync();

            // ASSERT
            response.Should().NotBeNull();
            response.Token.Should().NotBeNull();
        }

        [Theory]
        [InlineAutoMoqData("/Token")]
        public void GivenInvalidBasicToken_WithoutClient_ReturnsBadRequestResponse(
            string uri,
            string badApiBasicToken)
        {
            // ARRANGE
            var request = new RestRequest(uri, Method.POST);
            var body = new ApiTokenRequest { BasicToken = badApiBasicToken };
            request.AddParameter("text/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);

            var restClient = new RestClient(_apiBaseUrl);

            // ACT
            var response = restClient.Execute(request);

            // ASSERT
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}