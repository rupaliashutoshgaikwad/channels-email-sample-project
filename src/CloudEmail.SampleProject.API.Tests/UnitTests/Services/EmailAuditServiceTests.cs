using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using MimeKit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class EmailAuditServiceTests
    {
        [Theory]
        [AutoMoqData]
        public void LogCustomSmtpTestEmailStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ACT
            target.LogCustomSmtpTestEmailStart(emailId);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendEmailFromStorageStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ACT
            target.LogSendEmailFromStorageStart(emailId);
        }

        [Theory]
        [AutoMoqData]
        public void LogMimeBuilderLambdaFailure_GivenEmailIdAndResponse_LogsMessage(
            EmailAuditService target,
            string emailId,
            string response
        )
        {
            // ACT
            target.LogMimeBuilderLambdaFailure(emailId, response);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendRoutedEmailStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            string emailId,
            int businessUnit,
            List<string> tos,
            List<string> froms,
            string contactId,
            string fileServerVip
        )
        {
            // ACT
            target.LogSendRoutedEmailStart(emailId, businessUnit, tos, froms, contactId, fileServerVip);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendRoutedEmailSuccess_GivenEmailId_LogsMessage(
            EmailAuditService target,
            string emailId,
            int businessUnit,
            List<string> tos,
            List<string> froms,
            string contactId,
            string fileServerVip
        )
        {
            // ACT
            target.LogSendRoutedEmailSuccess(emailId, businessUnit, tos, froms, contactId, fileServerVip);
        }

        [Theory]
        [AutoMoqData]
        public void LogPutToCloudStorageQueueResult_GivenEmailIdAndSuccess_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ARRANGE
            bool putToCloudStorageQueueSuccessful = true;

            // ACT
            target.LogPutToCloudStorageQueueResult(emailId, putToCloudStorageQueueSuccessful);
        }

        [Theory]
        [AutoMoqData]
        public void LogPutToCloudStorageQueueResult_GivenEmailIdAndFailure_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ARRANGE
            bool putToCloudStorageQueueSuccessful = false;

            // ACT
            target.LogPutToCloudStorageQueueResult(emailId, putToCloudStorageQueueSuccessful);
        }

        [Theory]
        [AutoMoqData]
        public void LogPutToLogEmailQueueResult_GivenEmailIdAndSuccess_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ARRANGE
            bool putToLogEmailQueueSuccessful = true;

            // ACT
            target.LogPutToLogEmailQueueResult(emailId, putToLogEmailQueueSuccessful);
        }

        [Theory]
        [AutoMoqData]
        public void LogPutToLogEmailQueueResult_GivenEmailIdAndFailure_LogsMessage(
            EmailAuditService target,
            string emailId
        )
        {
            // ARRANGE
            bool putToLogEmailQueueSuccessful = false;

            // ACT
            target.LogPutToLogEmailQueueResult(emailId, putToLogEmailQueueSuccessful);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendSesStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            InternetAddressList to,
            InternetAddressList from,
            string emailId,
            string messageId,
            bool enforceTLS
        )
        {
            // ACT
            target.LogSendSesStart(to, from, emailId, messageId, enforceTLS);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendKerioStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            InternetAddressList to,
            InternetAddressList from,
            string emailId
        )
        {
            // ACT
            target.LogSendKerioStart(to, from, emailId);
        }

        [Theory]
        [AutoMoqData]
        public void LogSendCustomSmtpStart_GivenEmailId_LogsMessage(
            EmailAuditService target,
            InternetAddressList to,
            InternetAddressList from,
            string host,
            int port,
            string emailId
        )
        {
            // ACT
            target.LogSendCustomSmtpStart(to, from, host, port, emailId);
        }

        [Theory]
        [AutoMoqData]
        public void LogPutEmailToUnsendablesSuccess_GivenEmailId_LogsMessage(
            EmailAuditService target,
            string emailId,
            int businessUnit,
            string contactId
        )
        {
            // ACT
            target.LogPutEmailToUnsendablesSuccess(emailId, businessUnit, contactId);
        }
    }
}
