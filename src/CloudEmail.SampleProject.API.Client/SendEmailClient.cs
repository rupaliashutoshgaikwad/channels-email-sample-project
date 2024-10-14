using CloudEmail.API.Models.Responses;
using CloudEmail.ApiAuthentication;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.SampleProject.API.Client.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Client
{
    /// <summary>
    /// The send email client.
    /// </summary>
    public class SendEmailClient
        : BaseClient, ISendEmailClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendEmailClient"/> class.
        /// </summary>
        /// <param name="apiBaseUrl">The api base url.</param>
        /// <param name="basicToken">The basic token.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        public SendEmailClient(string apiBaseUrl, string basicToken, IHttpClientFactory httpClientFactory = null)
            : base(apiBaseUrl, basicToken, httpClientFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendEmailClient"/> class.
        /// </summary>
        /// <param name="apiBaseUrl">The api base url.</param>
        /// <param name="apiToken">The api token.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        public SendEmailClient(string apiBaseUrl, ApiToken apiToken, IHttpClientFactory httpClientFactory = null)
            : base(apiBaseUrl, apiToken, httpClientFactory)
        {
        }

        /// <inheritdoc/>
        public async Task<SendEmailResponse> SendEmailFromStorage(
            string emailId,
            int queueReceiveCount,
            string sentTimeStampString)
        {
            string uri = $"/SendEmail/SendEmailFromStorage?emailId={emailId}&queueReceiveCount={queueReceiveCount}&sentTimeStampString={sentTimeStampString}";
            using (var httpRespnse = await this.SendPostRequestAsync(uri))
            {
                return await this.DeserializeBodyAsync<SendEmailResponse>(httpRespnse);
            }
        }
    }
}