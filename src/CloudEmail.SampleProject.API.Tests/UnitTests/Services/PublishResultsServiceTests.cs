using AutoFixture.Xunit2;
using CloudEmail.API.Models;
using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Clients.Interfaces;
using CloudEmail.SampleProject.API.Services;
using MimeKit;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class PublishResultsServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailResults_GivenSuccessfulEmailSend_PublishMetrics(
            [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
                PublishResultsService target,
                SendEmailResponse sendEmailResponse,
                int busNo,
                string contactId,
                string emailId
            )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.Success;
            sendEmailResponse.EdgeType = EdgeType.SES;

            const string mattEmail = "matt@matt.com";
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse(mattEmail));

            // ACT
            await target.PublishSendEmailResults(
                sendEmailRequest,
                emailId,
                EdgeType.SES,
                sendEmailResponse,
                new MailAddress(mattEmail),
                mimeMessage);

            // ASSERT
            publishResultsClientMock.Verify(x => x.PublishSendEmailSuccess(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<EdgeType>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailResults_GivenFailedEmailSend_PublishMetrics(
            [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
                PublishResultsService target,
                SendEmailResponse sendEmailResponse,
                int busNo,
                string contactId,
                string emailId
            )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.Failure;
            sendEmailResponse.EdgeType = EdgeType.SES;

            const string mattEmail = "matt@matt.com";
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse(mattEmail));

            //// ACT
            await target.PublishSendEmailResults(
                sendEmailRequest,
                emailId,
                EdgeType.SES,
                sendEmailResponse,
                new MailAddress(mattEmail),
                mimeMessage);

            // ASSERT            
            publishResultsClientMock.Verify(x => x.PublishSendEmailFailure(It.IsAny<string>(), It.IsAny<EdgeType>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailResults_GivenSuccessfulEmailSend_ThrowPublishError(
                [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
                PublishResultsService target,
                SendEmailResponse sendEmailResponse,
                int busNo,
                string contactId,
                string emailId
            )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.Success;
            sendEmailResponse.EdgeType = EdgeType.SES;

            const string mattEmail = "matt@matt.com";
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse(mattEmail));

            publishResultsClientMock.Setup(p => p.PublishSendEmailSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<EdgeType>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Failed to send success results."));

            //// ACT
            await target.PublishSendEmailResults(
                sendEmailRequest,
                emailId,
                EdgeType.SES,
                sendEmailResponse,
                new MailAddress(mattEmail),
                mimeMessage);

            // ASSERT
            publishResultsClientMock.Verify(p => p.PublishSendEmailSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<EdgeType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailBlacklist_GivenBlacklistedEmailSend_PublishMetrics(
                [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
                PublishResultsService target,
                SendEmailResponse sendEmailResponse,
                int busNo,
                string contactId
            )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.FullyBlacklisted;
            sendEmailResponse.EdgeType = EdgeType.SES;

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse("matt@matt.com"));

            //// ACT
            await target.PublishSendEmailBlacklist(
                sendEmailRequest.BusinessUnit.ToString(),
                EdgeType.SES);

            // ASSERT
            publishResultsClientMock.Verify(x => x.PublishSendEmailFailure(It.IsAny<string>(), It.IsAny<EdgeType>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailBlacklist_GivenBlacklistedEmailSend_ThrowPublishError(
                [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
                PublishResultsService target,
                SendEmailResponse sendEmailResponse,
                int busNo,
                string contactId
            )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.FullyBlacklisted;
            sendEmailResponse.EdgeType = EdgeType.SES;

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse("matt@matt.com"));

            publishResultsClientMock.Setup(p => p.PublishSendEmailFailure(It.IsAny<string>(), It.IsAny<EdgeType>())).Throws(new Exception("Failed to send blacklist results."));

            //// ACT
            await target.PublishSendEmailBlacklist(
                sendEmailRequest.BusinessUnit.ToString(),
                EdgeType.SES);
            
            // ASSERT
            publishResultsClientMock.Verify(x => x.PublishSendEmailFailure(It.IsAny<string>(), It.IsAny<EdgeType>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailResults_GivenDroppedEmailSend_PublishMetrics(
            [Frozen] Mock<IPublishResultsClient> publishResultsClientMock,
            PublishResultsService target,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId
        )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = GetMimeWrapperNoAttachments()
            };

            sendEmailResponse.SendEmailResponseCode = SendEmailResponseCode.Dropped;
            sendEmailResponse.EdgeType = EdgeType.Custom;

            const string mattEmail = "matt@matt.com";
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            mimeMessage.From.Add(MailboxAddress.Parse(mattEmail));

            //// ACT
            await target.PublishSendEmailResults(
                sendEmailRequest,
                emailId,
                EdgeType.Custom,
                sendEmailResponse,
                new MailAddress(mattEmail),
                mimeMessage);

            // ASSERT            
            publishResultsClientMock.Verify(x => x.PublishSendEmailDropped(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private MimeWrapper GetMimeWrapperNoAttachments()
        {
            var wrapperAttachments = new List<WrapperAttachment>();
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(new MailboxAddress("david@david.com", "david@david.com"));
            mimeMessage.From.Add(new MailboxAddress("matt@matt.com", "matt@matt.com"));
            return new MimeWrapper(DateTime.Now, mimeMessage.To.Select(m => m.ToString()).ToList(), mimeMessage.From.Select(m => m.ToString()).ToList(), mimeMessage.Cc.Select(m => m.ToString()).ToList(), mimeMessage.Bcc.Select(m => m.ToString()).ToList(), mimeMessage.Subject, mimeMessage.TextBody, mimeMessage.HtmlBody, wrapperAttachments);
        }
    }
}
