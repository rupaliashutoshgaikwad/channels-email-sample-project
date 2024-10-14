using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Collections.Generic;
using System.Linq;

namespace CloudEmail.SampleProject.API.Services
{
    public class EmailAuditService : IEmailAuditService
    {
        private readonly ILogger<EmailAuditService> logger;

        public EmailAuditService(ILogger<EmailAuditService> logger)
        {
            this.logger = logger;
        }

        public void LogCustomSmtpTestEmailStart(string emailId)
        {
            logger.LogInformation($"SendCustomSmtpTestEmail > EmailId: {emailId}");
        }

        public void LogSendEmailFromStorageStart(string emailId)
        {
            logger.LogInformation($"SendEmailFromStorage > EmailId: {emailId}");
        }

        public void LogMimeBuilderLambdaFailure(string emailId, string response)
        {
            logger.LogError($"InvokeMimeBuilderLambda was not successful for EmailId: {emailId} > Response: {response}");
        }

        public void LogSendRoutedEmailStart(string emailId, int businessUnit, List<string> tos, List<string> froms, string contactId, string fileServerVip)
        {
            logger.LogInformation($"SendRoutedEmail Started > EmailId: {emailId} > BU: {businessUnit} > To: {tos.FirstOrDefault()} > From: {froms.FirstOrDefault()} > ContactId: {contactId} > FileServerVip: {fileServerVip}");
        }

        public void LogSendRoutedEmailSuccess(string emailId, int businessUnit, List<string> tos, List<string> froms, string contactId, string fileServerVip)
        {
            logger.LogInformation($"SendRoutedEmail was successful > EmailId: {emailId} > BU: {businessUnit} > To: {tos.FirstOrDefault()} > From: {froms.FirstOrDefault()} > ContactId: {contactId} > FileServerVip: {fileServerVip}");
        }

        public void LogPutToCloudStorageQueueResult(string emailId, bool putToCloudStorageQueueSuccessful)
        {
            if (putToCloudStorageQueueSuccessful)
            {
                logger.LogInformation($"Put to Cloud Storage queue was successful > EmailId: {emailId}");
            }
            else
            {
                logger.LogError($"Put to Cloud Storage queue has failed > EmailId: {emailId}");
            }
        }

        public void LogPutToLogEmailQueueResult(string emailId, bool putToLogEmailQueueSuccessful)
        {
            if (putToLogEmailQueueSuccessful)
            {
                logger.LogInformation($"Put to Log Email queue was successful > EmailId: {emailId}");
            }
            else
            {
                logger.LogError($"Put to Log Email queue has failed > EmailId: {emailId}");
            }
        }

        public void LogSendSesStart(InternetAddressList to, InternetAddressList from, string emailId, string messageId, bool enforceTLS)
        {
            logger.LogInformation($"SendSes > To: {to} > From: {from} > EmailId: {emailId} > MessageId: {messageId} > TLS Enforced: {enforceTLS}");
        }

        public void LogSendKerioStart(InternetAddressList to, InternetAddressList from, string emailId)
        {
            logger.LogInformation($"SendKerio > To: {to} > From: {from} > EmailId: {emailId}");
        }

        public void LogSendCustomSmtpStart(InternetAddressList to, InternetAddressList from, string host, int port, string emailId)
        {
            logger.LogInformation($"SendCustomSmtp > To: {to} > From: {from} > SettingsHost: {host} > Port: {port} > EmailId: {emailId}");
        }

        public void LogPutEmailToUnsendablesSuccess(string emailId, int businessUnit, string contactId)
        {
            logger.LogInformation($"Email put to unsendables. EmailId: {emailId} - BU: {businessUnit} - ContactId: {contactId}");
        }
    }
}
