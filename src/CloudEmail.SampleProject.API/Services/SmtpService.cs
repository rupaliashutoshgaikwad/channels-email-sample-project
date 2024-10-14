using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using CloudEmail.API.Models.Enums;
using CloudEmail.Common;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Extensions;
using CloudEmail.SampleProject.API.Mappings.Interfaces;
using CloudEmail.SampleProject.API.Services.Interface;
using CloudEmail.SampleProject.API.Wrappers.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SendEmailResponse = CloudEmail.API.Models.Responses.SendEmailResponse;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using CloudEmail.Metadata.Api.Client.Interfaces;
using CloudEmail.Metadata.Api.Model;
using CloudEmail.Common.Models;

namespace CloudEmail.SampleProject.API.Services
{
    public class SmtpService : ISmtpService
    {
        private readonly SmtpServiceConfiguration smtpServiceConfiguration;
        private readonly AmazonSESConfiguration amazonSESConfiguration;
        private readonly ILogger<SmtpService> logger;
        private readonly IAmazonSimpleEmailServiceV2 sesService;
        private readonly ISmtpClientWrapperFactory smtpClientWrapperFactory;
        private readonly ISecureSocketOptionsMapping secureSocketOptionsMapping;
        private readonly IEmailAuditService emailAuditService;
        private readonly IExceptionService exceptionService;

        /// <summary>
        /// The metadata Client
        /// </summary>
        private readonly IMetadataClient metadataClient;
        private readonly IMetadataTracker metadataTracker;

        /// <summary>
        /// The string builder.
        /// </summary>
        private readonly StringBuilder stringBuilder = new StringBuilder();

        public SmtpService(
            IOptions<SmtpServiceConfiguration> smtpServiceConfiguration,
            ILogger<SmtpService> logger,
            IAmazonSimpleEmailServiceV2 sesService,
            ISmtpClientWrapperFactory smtpClientWrapperFactory,
            ISecureSocketOptionsMapping secureSocketOptionsMapping,
            IEmailAuditService emailAuditService,
            IExceptionService exceptionService,
            IOptions<AmazonSESConfiguration> amazonSESConfiguration,
            IMetadataClientFactory metadataClientFactory,
            IMetadataTracker metadataTracker)
        {
            this.smtpServiceConfiguration = smtpServiceConfiguration.Value;
            this.logger = logger;
            this.sesService = sesService;
            this.smtpClientWrapperFactory = smtpClientWrapperFactory;
            this.secureSocketOptionsMapping = secureSocketOptionsMapping;
            this.emailAuditService = emailAuditService;
            this.exceptionService = exceptionService;
            this.amazonSESConfiguration = amazonSESConfiguration.Value;
            this.metadataClient = metadataClientFactory.CreateMetadataClient();
            this.metadataTracker = metadataTracker;
        }

        public async Task<SendEmailResponse> SendSes(MimeMessage mimeMessage, string emailId, bool enforceTLS)
        {
            try
            {
                mimeMessage.Headers.Add("X-SES-CONFIGURATION-SET", enforceTLS
                    ? amazonSESConfiguration.SesTLSConfigurationSet
                    : amazonSESConfiguration.SesConfigurationSet);
                using (var message = mimeMessage.GetStream())
                {
                    var sesRequest = new SendEmailRequest { Content = new EmailContent { Raw = new RawMessage { Data = message } } };
                    using (var sendConnectionTokenSource = new CancellationTokenSource())
                    {
                        var sendConnectionTimeoutInSeconds = 100;
                        sendConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(sendConnectionTimeoutInSeconds));
                        var sendResult = await sesService.SendEmailAsync(sesRequest, sendConnectionTokenSource.Token);
                        emailAuditService.LogSendSesStart(mimeMessage.To, mimeMessage.From, emailId, sendResult.MessageId, enforceTLS);

                        await metadataTracker.UpdateAsync(msg => 
                        {
                            msg.SesMessageId = sendResult.MessageId;
                            msg.Status = "Sent";
                            msg.ExtraInfoType = "EdgeType";
                            msg.ExtraInfo = "Ses";
                        });

                        return new SendEmailResponse
                        {
                            SendEmailResponseCode = SendEmailResponseCode.Success,
                            MessageId = sendResult.MessageId,
                            EdgeType = EdgeType.SES
                        };
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("not verified"))
                {
                    await metadataTracker.UpdateAsync(msg => 
                    {
                        msg.Status = "Failed";
                        msg.StatusReason = "UnverifiedDomain";
                        msg.ExtraInfoType = "EdgeType";
                        msg.ExtraInfo = "Ses";
                    });

                    logger.LogInformation($"Attempted to SendSes for EmailId {emailId} but failed with domain verification exception. To: {mimeMessage.To} From: {mimeMessage.From} Exception: {e}");
                    return new SendEmailResponse
                    {
                        SendEmailResponseCode = SendEmailResponseCode.DomainNotVerified,
                        EdgeType = EdgeType.SES,
                        ErrorMessage = e.Message
                    };
                }

                await metadataTracker.UpdateAsync(msg =>
                {
                    msg.Status = "Failed";
                    msg.StatusReason = $"Exception - {e.Message}";
                    msg.ExtraInfoType = "EdgeType";
                    msg.ExtraInfo = "Ses";
                });

                logger.LogError($"Failed during SendSes. EmailId: {emailId}. Exception: {e}", e);
                return new SendEmailResponse
                {
                    SendEmailResponseCode = SendEmailResponseCode.Failure,
                    EdgeType = EdgeType.SES,
                    ErrorMessage = e.Message
                };
            }
        }

        public async Task<SendEmailResponse> SendKerio(MimeMessage mimeMessage, string emailId)
        {
            var mailTransport = smtpClientWrapperFactory.CreateSmtpClientWrapper();

            try
            {
                using (mailTransport)
                {
                    emailAuditService.LogSendKerioStart(mimeMessage.To, mimeMessage.From, emailId);
                    using (var connectConnectionTokenSource = new CancellationTokenSource())
                    {
                        var connectConnectionTimeoutInSeconds = 20;
                        connectConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(connectConnectionTimeoutInSeconds));
                        await mailTransport.ConnectAsync(smtpServiceConfiguration.KerioHost, 25, SecureSocketOptions.None, connectConnectionTokenSource.Token);
                    }
                    using (var sendConnectionTokenSource = new CancellationTokenSource())
                    {
                        var sendConnectionTimeoutInSeconds = 100;
                        sendConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(sendConnectionTimeoutInSeconds));
                        await mailTransport.SendAsync(mimeMessage, sendConnectionTokenSource.Token);
                    }
                    using (var disconnectConnectionTokenSource = new CancellationTokenSource())
                    {
                        var disconnectConnectionTimeoutInSeconds = 20;
                        disconnectConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(disconnectConnectionTimeoutInSeconds));
                        await mailTransport.DisconnectAsync(true, disconnectConnectionTokenSource.Token);
                    }

                    await metadataTracker.UpdateAsync(msg => 
                    {
                        msg.Status = "Sent";
                        msg.ExtraInfoType = "EdgeType";
                        msg.ExtraInfo = "Kerio";
                    });

                    return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Success, MessageId = mimeMessage.MessageId, EdgeType = EdgeType.Kerio };
                }
            }
            catch (SmtpCommandException ex) when (ex.ErrorCode == SmtpErrorCode.SenderNotAccepted)
            {
                await metadataTracker.UpdateAsync(msg =>
                {
                    msg.Status = "Failed";
                    msg.StatusReason = "SenderNotAccepted";
                    msg.ExtraInfoType = "EdgeType";
                    msg.ExtraInfo = "Kerio";
                });

                logger.LogInformation($"Attempted to SendKerio for EmailId {emailId} but failed with SenderNotAccepted. To: {mimeMessage.To} From: {mimeMessage.From}");
                return new SendEmailResponse() { SendEmailResponseCode = SendEmailResponseCode.Dropped };
            }
            catch (Exception e)
            {
                await metadataTracker.UpdateAsync(msg =>
                {
                    msg.Status = "Failed";
                    msg.StatusReason = e.Message;
                    msg.ExtraInfoType = "EdgeType";
                    msg.ExtraInfo = "Kerio";
                });

                logger.LogError($"Failed during SendKerio. EmailId: {emailId}. Exception: {e}", e);
                return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Failure, EdgeType = EdgeType.Kerio, ErrorMessage = e.Message };
            }
            finally
            {
                if (mailTransport.IsConnected)
                {
                    await mailTransport.DisconnectAsync(true);
                }
            }
        }

        public async Task<SendEmailResponse> SendCustomSmtp(MimeMessage mimeMessage, string host, int port, TlsOption tlsOption, string username, string password, string emailId, int authenticationOptionId = 1, byte[] certificateData = null, int businessUnit = 0, EmailType emailType = EmailType.CallCenter)
        {
            var mailTransport = smtpClientWrapperFactory.CreateSmtpClientWrapper();

            try
            {
                emailAuditService.LogSendCustomSmtpStart(mimeMessage.To, mimeMessage.From, host, port, emailId);
                using (mailTransport)
                {
                    var connectionType = secureSocketOptionsMapping.SecureSocketOptionsMapper(tlsOption.Option);
                    using (var connectConnectionTokenSource = new CancellationTokenSource())
                    {
                        var connectConnectionTimeoutInSeconds = 20;
                        connectConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(connectConnectionTimeoutInSeconds));

                        if (authenticationOptionId == 2)
                        {
                            var certificateCollection = new X509Certificate2Collection();
                            certificateCollection.Add(new X509Certificate2(certificateData, password));

                            mailTransport.ClientCertificates = certificateCollection;
                            mailTransport.CheckCertificateRevocation = false;
                        }

                        await mailTransport.ConnectAsync(host, port, connectionType, connectConnectionTokenSource.Token);
                    }

                    if (!string.IsNullOrEmpty(username))
                    {
                        using (var authenticateConnectionTokenSource = new CancellationTokenSource())
                        {
                            var authenticateConnectionTimeoutInSeconds = 20;
                            authenticateConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(authenticateConnectionTimeoutInSeconds));
                            await mailTransport.AuthenticateAsync(Encoding.UTF8, new NetworkCredential(username, password), authenticateConnectionTokenSource.Token);
                        }
                    }

                    using (var sendConnectionTokenSource = new CancellationTokenSource())
                    {
                        var sendConnectionTimeoutInSeconds = 100;
                        sendConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(sendConnectionTimeoutInSeconds));
                        await mailTransport.SendAsync(mimeMessage, sendConnectionTokenSource.Token);
                    }

                    using (var disconnectConnectionTokenSource = new CancellationTokenSource())
                    {
                        var disconnectConnectionTimeoutInSeconds = 20;
                        disconnectConnectionTokenSource.CancelAfter(TimeSpan.FromSeconds(disconnectConnectionTimeoutInSeconds));
                        await mailTransport.DisconnectAsync(true, disconnectConnectionTokenSource.Token);
                    }

                    await this.LogSuccessSendCustomSmtpAsync(emailId, host, port, tlsOption, mimeMessage, businessUnit);
                    return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Success, MessageId = mimeMessage.MessageId, EdgeType = EdgeType.Custom };
                }
            }
            catch (Exception e) when (emailType == EmailType.Proactive)
            {
                if (mailTransport.IsConnected)
                {
                    await mailTransport.DisconnectAsync(true);
                }
                await this.LogFailedSendCustomSmtpAsync(emailId, host, port, tlsOption, mimeMessage, "Dropped", businessUnit, e);
                return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Dropped, EdgeType = EdgeType.Custom , ErrorMessage = e.Message};
            }
            catch (SmtpCommandException e)
                when (exceptionService.ValidateSmtpCommandException(e))
            {
                if (mailTransport.IsConnected)
                {
                    await mailTransport.DisconnectAsync(true);
                }
                await this.LogFailedSendCustomSmtpAsync(emailId, host, port, tlsOption, mimeMessage, "Unsendable", businessUnit, e);
                return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Unsendable, EdgeType = EdgeType.Custom, ErrorMessage = e.Message };
            }
            catch (Exception e)
            {
                if (mailTransport.IsConnected)
                {
                    await mailTransport.DisconnectAsync(true);
                }
                await this.LogFailedSendCustomSmtpAsync(
                    emailId, host, port, tlsOption, mimeMessage, "Unexpected exception", businessUnit, e);
                return new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Failure, EdgeType = EdgeType.Custom, ErrorMessage = e.Message };
            }
        }

        /// <summary>
        /// Logs the successful send custom smtp message.
        /// </summary>
        /// <param name="emailId">The email id.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="tlsOption">The tls option.</param>
        /// <param name="mimeMessage">The mime message.</param>
        /// <param name="businessUnit">The business unit id.</param>
        private async Task LogSuccessSendCustomSmtpAsync(
            string emailId, string host, int port, TlsOption tlsOption, MimeMessage mimeMessage, int businessUnit)
        {
            try
            {
                this.stringBuilder
                    .Append("Succeeded during SendCustomSmtp ");

                await this.AddAllFieldsToLogMessage(emailId, host, port, tlsOption, mimeMessage);
                this.logger.LogInformation(message: this.stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                this.logger.LogError(
                    "Unable to write out a successful SendCustomSmtp log with exception: {0}", ex.ToString());
            }
            finally
            {
                this.stringBuilder.Clear();
                await this.WriteToMetadataDatabaseAsync(
                    emailId,
                    "SmtpSendSucceeded",
                    "Custom smtp outbound succeeded",
                    "SmtpSendSucceeded",
                    "200",
                    businessUnit);
            }
        }

        /// <summary>
        /// Logs the failed send custom smtp message.
        /// </summary>
        /// <param name="emailId">The email id.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="tlsOption">The tls option.</param>
        /// <param name="mimeMessage">The mime message.</param>
        /// <param name="message">The message.</param>
        /// <param name="businessUnit">The business unit id.</param>
        /// <param name="exception">The exception.</param>
        private async Task LogFailedSendCustomSmtpAsync(
            string emailId,
            string host,
            int port,
            TlsOption tlsOption,
            MimeMessage mimeMessage,
            string message,
            int businessUnit,
            Exception exception)
        {
            try
            {
                this.stringBuilder
                    .Append("Failed during SendCustomSmtp with message: \"")
                    .Append(message)
                    .Append(" \"");

                await this.AddAllFieldsToLogMessage(emailId, host, port, tlsOption, mimeMessage);
                this.logger.LogError(message: this.stringBuilder.ToString(), exception: exception);
            }
            catch (Exception ex)
            {
                var aggregateException = new AggregateException(ex, exception);

                this.logger.LogError(
                    "Unable to write out a failure SendCustomSmtp log with message \"{0}\" exception: {1}",
                    message,
                    aggregateException.ToString());
            }
            finally
            {
                this.stringBuilder.Clear();

                int statusReason = 100;
                string extraInfoType = "ErrorMessage";

                try
                {
                    if (exception is SmtpCommandException smtpException)
                    {
                        statusReason = (int)smtpException.StatusCode;
                        extraInfoType = "SmtpResponseMessage";
                    }
                    else if (exception is AuthenticationException authException && authException.InnerException is SmtpCommandException smtpCommandEx)
                    {
                        statusReason = (int)smtpCommandEx.StatusCode;
                        extraInfoType = "SmtpResponseMessage";
                    }
                    else
                    {
                        this.logger.LogError(message: "Custom SMTP unknown exception type:" + exception.GetType());
                    }
                }
                catch(Exception ex)
                {
                    this.logger.LogError(message: "Unable to process custom smtp exception: {0}", ex.Message);
                }


                await this.WriteToMetadataDatabaseAsync(
                    emailId,
                    extraInfoType,
                    $"Custom SMTP outbound failed with message: {message} - {exception.Message}",
                    "SmtpSendFailed",
                    statusReason.ToString(),
                    businessUnit);
            }
        }

        /// <summary>
        /// Adds all the fields to log message.
        /// </summary>
        /// <param name="emailId">The email id.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="tlsOption">The tls option.</param>
        /// <param name="mimeMessage">The mime message.</param>
        /// <returns>A Task.</returns>
        private async Task AddAllFieldsToLogMessage(
            string emailId, string host, int port, TlsOption tlsOption, MimeMessage mimeMessage)
        {
            var emailMetadataSummary = await this.ReadFromMetadataDatabaseAsync(emailId);
            var domain = GetDomainFromEmailAddress(emailMetadataSummary?.PointOfContact);

            #pragma warning disable IDE0055
            AddFieldAndValueToLogMessage(   "BusinessUnitId"    , emailMetadataSummary?.BusinessUnitId  );
            AddFieldAndValueToLogMessage(   "TenantId"          , emailMetadataSummary?.TenantId        );
            AddFieldAndValueToLogMessage(   "ContactId"         , emailMetadataSummary?.ContactId       );
            AddFieldAndValueToLogMessage(   "SesId"             , emailMetadataSummary?.SesMessageId    );
            AddFieldAndValueToLogMessage(   "EmailType"         , emailMetadataSummary?.EmailType       );
            #pragma warning restore IDE0055

            #pragma warning disable IDE0055
            AddFieldAndValueToLogMessage(   "Domain"        , domain                );
            AddFieldAndValueToLogMessage(   "EmailId"       , emailId               );
            AddFieldAndValueToLogMessage(   "Host"          , host                  );
            AddFieldAndValueToLogMessage(   "Port"          , port                  );
            AddFieldAndValueToLogMessage(   "TlsOption"     , tlsOption?.Option     );
            AddFieldAndValueToLogMessage(   "ToAddresses"   , mimeMessage?.To       );
            AddFieldAndValueToLogMessage(   "FromAddress"   , mimeMessage?.From     );
            AddFieldAndValueToLogMessage(   "Subject"       , mimeMessage?.Subject  );
            #pragma warning restore IDE0055
        }

        /// <summary>
        /// Gets the domain from email address.
        /// </summary>
        /// <param name="emailAddress">The email address.</param>
        /// <returns>A ReadOnlyMemory.</returns>
        private static ReadOnlyMemory<char> GetDomainFromEmailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return ReadOnlyMemory<char>.Empty;
            }

            var domain = emailAddress.AsMemory();
            int domainIndex = emailAddress.IndexOf('@');
            if (domainIndex < 0)
            {
                return ReadOnlyMemory<char>.Empty;
            }

            return domain.Slice(domainIndex + 1);
        }

        /// <summary>
        /// Adds the field and value to log message.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldValue">The field value.</param>
        private void AddFieldAndValueToLogMessage(string fieldName, object fieldValue)
        {
            object value = fieldValue is null ? "null" : fieldValue;

            this.stringBuilder
                .Append(" | ")
                .Append(fieldName)
                .AppendFormat(": {0}", value);
        }

        /// <summary>
        /// Reads the email summary from metadata database async.
        /// </summary>
        /// <param name="emailId">The email id.</param>
        /// <returns>A Task.</returns>
        private async Task<EmailMetadataSummary> ReadFromMetadataDatabaseAsync(string emailId)
        {
            return await this.metadataClient.GetEmailMetadataSummaryAsync(emailId);
        }

        /// <summary>
        /// Writes the email metadata the to metadata database async.
        /// </summary>
        /// <param name="emailId">The email id.</param>
        /// <param name="extraInfoType">The extra info type</param>
        /// <param name="extraInfo">Detailed message with message and TLS options</param>
        /// <param name="status">The status.</param>
        /// <param name="statusReason">The status reason.</param>
        /// <param name="businessUnit">The business unit id.</param>
        /// <returns>A Task.</returns>
        private async Task<string> WriteToMetadataDatabaseAsync(
            string emailId, string extraInfoType, string extraInfo, string status, string statusReason, int businessUnit)
        {
            var emailMetadata = new Metadata.Api.Model.EmailMetadata
            {
                EmailId = emailId,
                MetadataTimestamp = DateTime.UtcNow,
                Status = status,
                StatusReason = statusReason,
                Direction = "Outbound",
                ExtraInfoType = extraInfoType,
                ExtraInfo = extraInfo,
                BusinessUnitId = businessUnit
            };

            return await this.metadataClient.AddEmailAsync(emailMetadata);
        }
    }
}