using CloudEmail.ApiAuthentication;
using CloudEmail.SampleProject.API.Automation.Builders.Interfaces;
using FluentAssertions;
using RestSharp;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Automation.Builders
{
    public class RestRequestBuilder : IRestRequestBuilder
    {
        private readonly string _baseUrl;
        private readonly string _basicToken;

        public RestRequestBuilder(string baseUrl, string basicToken)
        {
            _baseUrl = baseUrl;
            _basicToken = basicToken;
        }

        public async Task<RestRequest> CreateGetRestRequestWithHeader(string uri)
        {
            var token = await GetApiToken();
            var request = new RestRequest(uri, Method.GET);
            request.AddHeader("Authorization", $"{token}");
            return request;
        }

        public async Task<RestRequest> CreatePostRestRequestWithHeader(string uri)
        {
            var token = await GetApiToken();
            var request = new RestRequest(uri, Method.POST);
            request.AddHeader("Authorization", $"{token}");
            return request;
        }

        public async Task<RestRequest> CreateDeleteRestRequestWithHeader(string uri)
        {
            var token = await GetApiToken();
            var request = new RestRequest(uri, Method.DELETE);
            request.AddHeader("Authorization", $"{token}");
            return request;
        }

        public async Task<RestRequest> CreateRestRequestWithHeader(string uri, Method restMethod)
        {
            var token = await GetApiToken();
            var request = new RestRequest(uri, restMethod);
            request.AddHeader("Authorization", $"{token}");
            return request;
        }

        private async Task<string> GetApiToken()
        {
            var tokenClient = ApiTokenProvider.Create(this._baseUrl, this._basicToken);
            var apiToken = await tokenClient.GetTokenAsync();

            apiToken.Should().NotBeNull();
            return apiToken.Token;
        }
    }
}