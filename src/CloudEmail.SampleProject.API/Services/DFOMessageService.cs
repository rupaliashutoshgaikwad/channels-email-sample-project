using Api.Model.Integration;
using Api.Model.Requests.Messages;
using Channels.DFO.Api.Client.Services;
using Channels.DFO.Api.Client.Services.Interfaces;
using Channels.DFO.Api.Model.Integration;
using Channels.DFO.Api.Model.Requests;
using Channels.DFO.Api.Model.Requests.Messages;
using Channels.UH.Token.Services;
using Channels.UH.Token.Services.Interfaces;
using Channels.UH.Token.Services.Model;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class DFOMessageService : IDFOMessageService
    {
        private readonly IConfiguration configuration;
        private IMessageService dfoMessageClient { get; }
        private IServiceTokenService serviceTokenService { get; }

        public DFOMessageService(
            IConfiguration configuration,
            IMessageService dfoMessageClient,
            IServiceTokenService serviceTokenService
            )
        {
            this.configuration = configuration;
            this.dfoMessageClient = dfoMessageClient;
            this.serviceTokenService = serviceTokenService;
        }

        public async Task<bool> UpdateMessageAttribute(string channelId, string messageIdOnExternalPlatform, string sendMessageId, string tenantId)
        {
            var dfoAuthorization = await GetDfoAuthorization(tenantId);

            var getMessagesResponse = await dfoMessageClient.UpdateMessageExternalAttribute(
                new PatchMessageExternalAttributesRequest
                {
                    Authorization = dfoAuthorization,
                    channelId = channelId,
                    messageIdOnExternalPlatform = messageIdOnExternalPlatform,
                    ExternalAttribute = new ExternalAttribute
                    {
                        ExternalAttributes = new Dictionary<string, string>
                        {
                            { 
                                "in-reply-to", sendMessageId 
                            }
                        }
                    }
                }
            );
            return getMessagesResponse;
        }

        private async Task<RequestAuthorization> GetDfoAuthorization(string tenantId)
        {
            ServiceToken serviceToken = await serviceTokenService.GetServiceToken(
                new Credentials
                {
                    ClientId = configuration.GetValue<string>("UserHub:ServiceUser:ClientId", string.Empty),
                    ClientSecret = configuration.GetValue<string>("UserHub:ServiceUser:ClientSecret", string.Empty)
                }
            );

            RequestAuthorization dfoAuthorization = new RequestAuthorization
            {
                TokenType = AuthorizationTokenType.Bearer,
                Token = serviceToken.AccessToken,
                TenantId = tenantId
            };

            return dfoAuthorization;
        }
    }
}
