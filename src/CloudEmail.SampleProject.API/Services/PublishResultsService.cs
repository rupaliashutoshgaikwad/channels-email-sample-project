using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using CloudEmail.SampleProject.API.Clients.Interfaces;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class PublishResultsService : IPublishResultsService
    {
        private readonly ILogger<PublishResultsService> _logger;
        private readonly IPublishResultsClient _publishResultsClient;

        public PublishResultsService(
            ILogger<PublishResultsService> logger,
            IPublishResultsClient publishResultsClient)
        {
            _logger = logger;
            _publishResultsClient = publishResultsClient;
        }

        public async Task PublishSendEmailResults(
            SendEmailRequest sendEmailRequest,
            string emailId,
            EdgeType edgeType,
            SendEmailResponse sendEmailResponse,
            MailAddress from,
            MimeMessage mimeMessage)
        {
            try
            {
                switch (sendEmailResponse.SendEmailResponseCode)
                {
                    case SendEmailResponseCode.Success:
                        {
                            var toAddresses = string.Join(",", mimeMessage.To.Mailboxes.Select(mbx => mbx.Address).ToList());
                            await _publishResultsClient.PublishSendEmailSuccess(
                                emailId,
                                mimeMessage.MessageId,
                                sendEmailRequest.BusinessUnit.ToString(),
                                edgeType,
                                from.ToString(),
                                toAddresses);
                            break;
                        }
                    case SendEmailResponseCode.Dropped:
                        {
                            await _publishResultsClient.PublishSendEmailDropped(from.Host, sendEmailRequest.BusinessUnit.ToString());
                            break;
                        }
                    default:
                        {
                            await _publishResultsClient.PublishSendEmailFailure(
                                sendEmailRequest.BusinessUnit.ToString(),
                                edgeType);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during PublishSendEmailResults: {e}", e);
            }
        }

        public async Task PublishSendEmailBlacklist(string businessUnit, EdgeType edgeType)
        {
            try
            {
                await _publishResultsClient.PublishSendEmailFailure(businessUnit, edgeType);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during PublishSendEmailBlacklist: {e}", e);
            }
        }
    }
}
