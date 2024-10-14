using CloudEmail.ApiAuthentication;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.SampleProject.API.Client.Interfaces;
using CloudEmail.SampleProject.API.Models.Requests;
using CloudEmail.SampleProject.API.Models.Responses;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Client
{
    /// <summary>
    /// The custom smtp configuration client.
    /// </summary>
    public class CustomSmtpConfigurationClient
        : BaseClient, ICustomSmtpConfigurationClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSmtpConfigurationClient"/> class.
        /// </summary>
        /// <param name="apiBaseUrl">The api base url.</param>
        /// <param name="basicToken">The basic token.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        public CustomSmtpConfigurationClient(string apiBaseUrl, string basicToken, IHttpClientFactory httpClientFactory = null)
            : base(apiBaseUrl, basicToken, httpClientFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSmtpConfigurationClient"/> class.
        /// </summary>
        /// <param name="apiBaseUrl">The api base url.</param>
        /// <param name="apiToken">The api token.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        public CustomSmtpConfigurationClient(string apiBaseUrl, ApiToken apiToken, IHttpClientFactory httpClientFactory = null)
            : base(apiBaseUrl, apiToken, httpClientFactory)
        {
        }

        /// <inheritdoc/>
        public async Task<TestCustomSmtpConfigurationResponse> SendTestEmail(TestCustomSmtpConfigurationRequest request)
        {
            using (var httpClient = base.CreateHttpClient())
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{base._apiBaseUrl}/CustomSmtpConfiguration/SendTestEmail"))
            {
                httpRequest.Headers.Authorization = await base.GetAuthenticationHeaderAsync();
                httpRequest.Content = JsonContent.Create(request);

                using (var httpResponse = await httpClient.SendAsync(httpRequest))
                {
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<TestCustomSmtpConfigurationResponse>(
                            await httpResponse.Content.ReadAsStringAsync());
                    }

                    return new TestCustomSmtpConfigurationResponse
                    {
                        TestEmailSentSuccessfully = false,
                        ErrorMessage = httpResponse.ReasonPhrase
                    };
                }
            }
        }
    }
}