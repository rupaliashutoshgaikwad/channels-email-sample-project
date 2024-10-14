using AutoMapper;
using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using CloudEmail.Mime.Libraries.Models;
using CloudEmail.Mime.Libraries.Services.Interfaces;
using CloudEmail.SampleProject.API.Extensions;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class SendEmailService : ISendEmailService
    {
        private readonly ISmtpService smtpService;
        private readonly IBlacklistService blacklistService;
        private readonly IPublishResultsService publishResultsService;
        private readonly ICustomSmtpConfigurationService customSmtpConfigurationService;
        private readonly IMimeService mimeService;
        private readonly ILogger<SendEmailService> logger;
        private readonly IDomainVerificationService domainVerificationService;
        private readonly IDFOMessageService dfoMessageService;
        private readonly IMapper mapper;
        private readonly IFeatureToggleService featureToggleService;

        private readonly long sesMaxAllowedLength = 41943040;
        private readonly long kerioMaxAllowedLength = 41943040;
        private readonly int queueLimit = 3;

        public SendEmailService(
            ISmtpService smtpService,
            IBlacklistService blacklistService,
            IPublishResultsService publishResultsService,
            ICustomSmtpConfigurationService customSmtpConfigurationService,
            IMimeService mimeService,
            ILogger<SendEmailService> logger,
            IDomainVerificationService domainVerificationService,
            IMapper mapper,
            IFeatureToggleService featureToggleService,
            IDFOMessageService dfoMessageService
            )
        {
            this.smtpService = smtpService;
            this.blacklistService = blacklistService;
            this.publishResultsService = publishResultsService;
            this.customSmtpConfigurationService = customSmtpConfigurationService;
            this.mimeService = mimeService;
            this.logger = logger;
            this.domainVerificationService = domainVerificationService;
            this.mapper = mapper;
            this.featureToggleService = featureToggleService;
            this.dfoMessageService = dfoMessageService;
        }

        public async Task<SendEmailResponse> SendRoutedEmail(SendEmailRequest sendEmailRequest, int queueReceiveCount)
        {
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(sendEmailRequest.MimeWrapper);

            var mime = mimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);



            var encodingMethods = mime.Attachments.SelectMany(a => a.ContentDisposition.Parameters.Select(p => nameof(p.EncodingMethod)));
            logger.LogInformation($"[SendRoutedEmail] MimeMessage disposition parameter encoding methods: {string.Join(", ", encodingMethods)}");

            var fromMailAddress = new MailAddress(mime.From.Mailboxes.First().Address);

            MailboxAddress.TryParse(mime.From.ToString(), out var domainAddress);
            var domainAddress2 = domainAddress.Address.Substring(domainAddress.Address.LastIndexOf('@') + 1);
            
            var disableKerio = await featureToggleService.GetFeatureToggle("disable_kerio");

            var customSmtpSettings = await customSmtpConfigurationService.GetCustomSmtpConfiguration(sendEmailRequest.BusinessUnit, domainAddress2);

            if (customSmtpSettings != null && customSmtpSettings.Enabled)
            {
                logger.LogInformation($"Custom SMTP Server Path {customSmtpSettings.AuthenticationOption} and {customSmtpSettings.CertificateData}");

                if (sendEmailRequest.EmailType.HasValue && sendEmailRequest.EmailType.Value != EmailType.CallCenter && sendEmailRequest.EmailType.Value != EmailType.Proactive)
                {
                    if (!disableKerio)
                    {
                        logger.LogInformation($"Email with EmailId {sendEmailRequest.EmailId} is not a call center email. Will send through Kerio instead. " +
                            $"First To Address: {mime.To.FirstOrDefault()} First From Address: {mime.From.FirstOrDefault()}");
                        var kerioResponse = await smtpService.SendKerio(mime, sendEmailRequest.EmailId);
                        await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.Kerio, kerioResponse, fromMailAddress, mime);
                        return kerioResponse;
                    }
                    else
                    {
                        logger.LogInformation($"Email with EmailId {sendEmailRequest.EmailId} is not a call center email and kerio is disabled. Will try to send through SES instead. " +
                            $"First To Address: {mime.To.FirstOrDefault()} First From Address: {mime.From.FirstOrDefault()}");
                    }
                }
                else
                {
                    logger.LogInformation($"Sending Email Id: {sendEmailRequest.EmailId} with Domain: {domainAddress2} from Custom SMTP Host: {customSmtpSettings.Host}");
                    var sendCustomResponse = await smtpService.SendCustomSmtp(mime, customSmtpSettings.Host, customSmtpSettings.Port, customSmtpSettings.TlsOption, customSmtpSettings.Username, customSmtpSettings.Password, sendEmailRequest.EmailId, customSmtpSettings.AuthenticationOption.Id, customSmtpSettings.CertificateData, sendEmailRequest.BusinessUnit, sendEmailRequest.EmailType ?? EmailType.CallCenter);
                    await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.Custom, sendCustomResponse, fromMailAddress, mime);
                    logger.LogInformation($"Custom SMTP end");
                    return sendCustomResponse;
                }
            }
            else if (sendEmailRequest.EmailType == EmailType.Proactive)
            {
                logger.LogInformation($"No custom SMTP found for proactive email with EmailId {sendEmailRequest.EmailId}");
                var sendEmailResponse = new SendEmailResponse
                {
                    SendEmailResponseCode = SendEmailResponseCode.Dropped,
                    EmailId = sendEmailRequest.EmailId,
                    EdgeType = EdgeType.Custom,
                    ErrorMessage = "Proactive email must go out custom SMTP."
                };
                return sendEmailResponse;
            }

            /* 
             * Removing this call to avoid unnecessary call as we noticed there are no entries in prodcution DB tables 
             * And will be handled with DE-29799
            */
            //blacklistService.RemoveBlacklistedRecipients(mime, sendEmailRequest.EmailId);

            var allRecipientsBlacklisted = !mime.To.Any() && !mime.Cc.Any() && !mime.Bcc.Any();
            if (allRecipientsBlacklisted)
            {
                await publishResultsService.PublishSendEmailBlacklist(sendEmailRequest.BusinessUnit.ToString(), EdgeType.SES);
                return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.FullyBlacklisted };
            }

            using (var message = mime.GetStream())
            {
                var isDomainVerified = false;
                if (MailboxAddress.TryParse(mime.From.ToString(), out var fromAddress))
                {
                    var domain = fromAddress.Address.Substring(fromAddress.Address.LastIndexOf('@') + 1);
                    isDomainVerified = await domainVerificationService.IsDomainVerified(domain);
                }

                if (!isDomainVerified)
                {
                    if (!disableKerio)
                    {
                        logger.LogInformation($"Email with EmailId {sendEmailRequest.EmailId} is not sending from a verified domain. Will send through Kerio instead. " +
                            $"First To Address: {mime.To.FirstOrDefault()} First From Address: {mime.From.FirstOrDefault()}");
                    }
                    else {                         
                        logger.LogInformation($"Email with EmailId {sendEmailRequest.EmailId} is not sending from a verified domain and kerio is disabled. Will not send. " +
                            $"First To Address: {mime.To.FirstOrDefault()} First From Address: {mime.From.FirstOrDefault()}");
                            var emailResponse = new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Dropped, EdgeType = EdgeType.Custom, ErrorMessage = "Domain not verified" };
                            await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.SES, emailResponse, fromMailAddress, mime);
                            return emailResponse;
                        }
                }
                else if (queueReceiveCount == queueLimit && !disableKerio)
                {
                    logger.LogInformation($"Email with EmailId {sendEmailRequest.EmailId} has been re-queued twice to send via SES but was unsuccessful. Will send through Kerio instead. " +
                        $"First To Address: {mime.To.FirstOrDefault()} First From Address: {mime.From.FirstOrDefault()}");
                }
                else 
                {
                    // SES has a limit of 40MB for the entire email message.
                    if (message.Length > sesMaxAllowedLength)
                    {
                        var logInfo = $"Large Email - Email with EmailId {sendEmailRequest.EmailId} is too large to send through SES. " +
                        $"Message length: {message.Length} bytes. SES Max Length: 41943040 bytes or 40MB. First To Address: {mime.To.FirstOrDefault()} " +
                        $"First From Address: {mime.From.FirstOrDefault()} Contact Id: {sendEmailRequest.ContactId} Domain verified: true";
                        logger.LogInformation(logInfo);
                        var emailResponse = new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Unsendable, EdgeType = EdgeType.Custom, ErrorMessage = logInfo };
                        await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.SES, emailResponse, fromMailAddress, mime);
                        return emailResponse;
                    }
                    else
                    {
                        var sesResponse = await smtpService.SendSes(mime, sendEmailRequest.EmailId, sendEmailRequest.EnforceTLS);

                        if (sesResponse.SendEmailResponseCode != SendEmailResponseCode.DomainNotVerified)
                        {
                            await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.SES, sesResponse, fromMailAddress, mime);
                            return sesResponse;
                        }
                    }
                }
            }
            using (var message = mime.GetStream())
            {
                // Kerio has a limit of 40MB for the entire email message.
                if (message.Length > kerioMaxAllowedLength)
                {
                    var logInfo = $"Large Email - Email with EmailId {sendEmailRequest.EmailId} is too large to send through Kerio. " +
                        $"Message length: {message.Length} bytes. Kerio Max Length: 41943040 bytes or 40MB. First To Address: {mime.To.FirstOrDefault()} " +
                        $"First From Address: {mime.From.FirstOrDefault()} Contact Id: {sendEmailRequest.ContactId} Domain verified: false";
                    logger.LogInformation(logInfo);
                    var emailResponse = new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Unsendable, EdgeType = EdgeType.Custom, ErrorMessage = logInfo };
                    await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.Kerio, emailResponse, fromMailAddress, mime);
                    return emailResponse;
                }
                else
                {
                    var kerioResponse = await smtpService.SendKerio(mime, sendEmailRequest.EmailId);
                    await publishResultsService.PublishSendEmailResults(sendEmailRequest, sendEmailRequest.EmailId, EdgeType.Kerio, kerioResponse, fromMailAddress, mime);
                    return kerioResponse;
                }
            }
        }
        public async Task<bool> UpdateDFOExternalAttribute(SendEmailRequest sendEmailRequest, SendEmailResponse sendEmailResponse)
        {
            try
            {
                if (sendEmailRequest.isDFOMail && !string.IsNullOrEmpty(sendEmailRequest.DfoChannelId))
                {
                    logger.LogInformation($"Calling DFO api to update the external attribute EmailId: {sendEmailRequest.EmailId}, MessageId: {sendEmailResponse.MessageId}, ChannelId: {sendEmailRequest.DfoChannelId}");

                    return await dfoMessageService.UpdateMessageAttribute(sendEmailRequest.DfoChannelId, sendEmailRequest.EmailId, sendEmailResponse.MessageId.Split('@')[0], sendEmailRequest.DfoTenantId);
                }
            }
            catch
            {
                logger.LogError($"Failed to update DFO external attribute for EmailId: {sendEmailRequest.EmailId}");
            }
            return false;
        }
    }
}
