using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using AutoFixture.Xunit2;
using CloudEmail.API.Models.Enums;
using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Mappings.Interfaces;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.SampleProject.API.Services.Interface;
using CloudEmail.SampleProject.API.Wrappers;
using CloudEmail.SampleProject.API.Wrappers.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using System.Reflection;
using System.IO;
using CloudEmail.SampleProject.API.Configuration;
using Microsoft.Extensions.Options;
using CloudEmail.Metadata.Api.Client.Interfaces;
using CloudEmail.Metadata.Api.Model;
using Microsoft.Extensions.Logging;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class SmtpServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task SendSes_GivenSuccessfulSendRawEmail_ReturnsSuccessResponse(
            [Frozen] Mock<IAmazonSimpleEmailServiceV2> simpleEmailServiceMock,
            [Frozen] Mock<IOptions<AmazonSESConfiguration>> amazoneSESConfigurationMock,
            SendEmailResponse sendRawEmailResponse,
            SmtpService smtpService,
            string emailId
        )
        {
            // ARRANGE
            sendRawEmailResponse.HttpStatusCode = (HttpStatusCode)200;
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            simpleEmailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendRawEmailResponse);

            amazoneSESConfigurationMock
                .Setup(x => x.Value.SesConfigurationSet).Returns("");

            // ACT
            var result = await smtpService.SendSes(mimeMessage, emailId, false);

            // ASSERT
            simpleEmailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.SES, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendKerio_GivenSuccessfulConnectionAndValidMessage_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            // ACT
            var result = await target.SendKerio(mimeMessage, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Kerio, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenValidInputNoAuth_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = null;
            const string password = null;

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Never);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Custom, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenValidInputNoAuthWithSslEnabled_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = null;
            const string password = null;

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Never);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Custom, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenValidInputWithAuth_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Custom, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenSendException_ReturnFailureResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws<Exception>();
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenProactiveEmail_ReturnDroppedResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws<Exception>();
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.Proactive);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Dropped, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustomSmtp_ValidateSmtpCommandExceptionTrue_ReturnUnsendableResponse(
           [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
           [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
           [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
           [Frozen] Mock<IExceptionService> exceptionServiceMock,
           SecureSocketOptions option,
           TlsOption tlsOption,
           SmtpService target,
           string emailId
       )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("test@nodomain.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(),
                It.IsAny<ITransferProgress>())).Throws(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted,
                SmtpStatusCode.ErrorInProcessing,
                "Sender address <test@nodomain.com> domain does not exist")); 
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            exceptionServiceMock
                .Setup(x => x.ValidateSmtpCommandException(It.IsAny<SmtpCommandException>())).Returns(true);
            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Unsendable, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustomSmtp_ValidateSmtpCommandExceptionFalse_ReturnFailureResponse(
           [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
           [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
           [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
           [Frozen] Mock<IExceptionService> exceptionServiceMock,
           SecureSocketOptions option,
           TlsOption tlsOption,
           SmtpService target,
           string emailId
       )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("test@somedomain.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(),
                It.IsAny<ITransferProgress>())).Throws(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted,
                SmtpStatusCode.ErrorInProcessing,
                "Failed with SmtpCommandException"));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            exceptionServiceMock
                .Setup(x => x.ValidateSmtpCommandException(It.IsAny<SmtpCommandException>())).Returns(false);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustomSmtp_NormalException_ReturnFailureResponse(
           [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
           [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
           [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
           SecureSocketOptions option,
           TlsOption tlsOption,
           SmtpService target,
           string emailId
       )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws(new Exception("Failed"));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendKerio_GivenSendException_ReturnFailureResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws<Exception>();
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            // ACT
            var result = await target.SendKerio(mimeMessage, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()),
                Times.Exactly(1));
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendSes_GivenSendRawEmailException_ReturnFailureResponse(
            [Frozen] Mock<IAmazonSimpleEmailServiceV2> simpleEmailServiceMock,
            [Frozen] Mock<IOptions<AmazonSESConfiguration>> amazoneSESConfigurationMock,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            simpleEmailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
                .Throws<Exception>();

            amazoneSESConfigurationMock
                .Setup(x => x.Value.SesConfigurationSet).Returns("");

            // ACT
            var result = await target.SendSes(mimeMessage, emailId, false);

            // ASSERT
            simpleEmailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendSes_GivenDomainNotVerifiedException_ReturnDomainNotVerifiedResponse(
            [Frozen] Mock<IAmazonSimpleEmailServiceV2> simpleEmailServiceMock,
            [Frozen] Mock<IOptions<AmazonSESConfiguration>> amazoneSESConfigurationMock,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            var exception = new Exception("Domain is not verified.");
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            simpleEmailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
                .Throws(exception);

            amazoneSESConfigurationMock
                .Setup(x => x.Value.SesConfigurationSet).Returns("");

            // ACT
            var result = await target.SendSes(mimeMessage, emailId, false);

            // ASSERT
            simpleEmailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(SendEmailResponseCode.DomainNotVerified, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendKerio_GivenSmtpCommandException_ReturnsDroppedResponse(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            SmtpService target,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws(new SmtpCommandException(SmtpErrorCode.SenderNotAccepted, SmtpStatusCode.SystemStatus, string.Empty));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            // ACT
            var result = await target.SendKerio(mimeMessage, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()),
                Times.Exactly(1));
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(SendEmailResponseCode.Dropped, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_GivenValidInputWithCertificateAuth_ReturnsSuccessResponse(
        [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
        [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
        [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
        SecureSocketOptions option,
        TlsOption tlsOption,
        SmtpService target,
        string emailId
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string password = "password";
            const int authOption = 2;
            byte[] embeddedCert;
            Assembly thisAssembly = Assembly.GetAssembly(typeof(SmtpServiceTests));
            using (Stream certStream = thisAssembly.GetManifestResourceStream("CloudEmail.SampleProject.API.Tests.UnitTests.Services._fixtures.testCert.cer"))
            {
                embeddedCert = new byte[certStream.Length];
                certStream.Read(embeddedCert, 0, (int)certStream.Length);
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, "", password, emailId, authOption, embeddedCert);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Custom, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendSes_ShouldAddSesConfigurationSetToHeader(
            [Frozen] Mock<IAmazonSimpleEmailServiceV2> simpleEmailServiceMock,
            [Frozen] Mock<IOptions<AmazonSESConfiguration>> amazoneSESConfigurationMock,
            SendEmailResponse sendRawEmailResponse,
            SmtpService smtpService,
            string emailId
        )
        {
            // ARRANGE
            sendRawEmailResponse.HttpStatusCode = (HttpStatusCode)200;
            var mimeMessage = new MimeMessage();
            var fakeConfigSetName = "fake-config-set";
            mimeMessage.To.Add(MailboxAddress.Parse("sg@sestest.com"));
            simpleEmailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest >(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendRawEmailResponse);

            amazoneSESConfigurationMock
                .Setup(x => x.Value.SesConfigurationSet).Returns(fakeConfigSetName);

            // ACT
            var result = await smtpService.SendSes(mimeMessage, emailId, false);

            // ASSERT
            simpleEmailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1));
            Assert.Equal(fakeConfigSetName, mimeMessage.Headers["X-SES-CONFIGURATION-SET"]);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.SES, result.EdgeType);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_CommonException_ShouldCallMetaDataAPIWithFailedStatusDataz(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            [Frozen] Mock<IMetadataClient> metaDataClientMock,
            [Frozen] Mock<IMetadataClientFactory> metadataClientFactoryMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            string emailId,
            IOptions<SmtpServiceConfiguration> smtpOptions,
            ILogger<SmtpService> logger,
            IAmazonSimpleEmailServiceV2 sesService,
            IEmailAuditService emailAuditService,
            IExceptionService exceptionService,
            IOptions<AmazonSESConfiguration> sesOptions,
            IMetadataTracker metadataTracker
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Throws<Exception>();
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            metaDataClientMock.Setup(x => x.AddEmailAsync(It.IsAny<EmailMetadata>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            metadataClientFactoryMock.Setup(x => x.CreateMetadataClient()).Returns(metaDataClientMock.Object);

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            SmtpService target = new SmtpService(smtpOptions,
            logger,
            sesService,
            smtpClientWrapperFactoryMock.Object,
            secureSocketOptionsMappingMock.Object,
            emailAuditService,
            exceptionService,
            sesOptions,
            metadataClientFactoryMock.Object,
            metadataTracker);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            metaDataClientMock.Verify(x => x.AddEmailAsync(
                It.Is<EmailMetadata>(m =>
                m.StatusReason.Equals("100") &&
                m.ExtraInfoType.Equals("ErrorMessage") &&
                m.Status.Equals("SmtpSendFailed")
                ), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_SMTPCommandException_ShouldCallMetaDataAPIWithFailedStatusDataz(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            [Frozen] Mock<IMetadataClient> metaDataClientMock,
            [Frozen] Mock<IMetadataClientFactory> metadataClientFactoryMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            string emailId,
            IOptions<SmtpServiceConfiguration> smtpOptions,
            ILogger<SmtpService> logger,
            IAmazonSimpleEmailServiceV2 sesService,
            IEmailAuditService emailAuditService,
            IExceptionService exceptionService,
            IOptions<AmazonSESConfiguration> sesOptions,
            IMetadataTracker metadataTracker
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Throws(new SmtpCommandException(SmtpErrorCode.SenderNotAccepted, SmtpStatusCode.TransactionFailed, "5.2.252 SendAsDenied; mail@domain.com not allowed to send as sendaddress@fromdomain.com"));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            metaDataClientMock.Setup(x => x.AddEmailAsync(It.IsAny<EmailMetadata>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            metadataClientFactoryMock.Setup(x => x.CreateMetadataClient()).Returns(metaDataClientMock.Object);

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            SmtpService target = new SmtpService(smtpOptions,
            logger,
            sesService,
            smtpClientWrapperFactoryMock.Object,
            secureSocketOptionsMappingMock.Object,
            emailAuditService,
            exceptionService,
            sesOptions,
            metadataClientFactoryMock.Object,
            metadataTracker);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            metaDataClientMock.Verify(x => x.AddEmailAsync(
                It.Is<EmailMetadata>(m =>
                m.StatusReason.Equals("554") &&
                m.ExtraInfoType.Equals("SmtpResponseMessage") &&
                m.Status.Equals("SmtpSendFailed")
                ), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_AuthenticationException_ShouldCallMetaDataAPIWithFailedStatusDataz(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            [Frozen] Mock<IMetadataClient> metaDataClientMock,
            [Frozen] Mock<IMetadataClientFactory> metadataClientFactoryMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            string emailId,
            IOptions<SmtpServiceConfiguration> smtpOptions,
            ILogger<SmtpService> logger,
            IAmazonSimpleEmailServiceV2 sesService,
            IEmailAuditService emailAuditService,
            IExceptionService exceptionService,
            IOptions<AmazonSESConfiguration> sesOptions,
            IMetadataTracker metadataTracker
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Throws(new AuthenticationException("535: 5.7.139 Authentication unsuccessful, the user credentials were incorrect.",
                new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.AuthenticationInvalidCredentials, "535: 5.7.139 Authentication unsuccessful, the user credentials were incorrect.")));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            metaDataClientMock.Setup(x => x.AddEmailAsync(It.IsAny<EmailMetadata>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            metadataClientFactoryMock.Setup(x => x.CreateMetadataClient()).Returns(metaDataClientMock.Object);

            mailTransportMock
                .Setup(x => x.IsConnected).Returns(true);

            SmtpService target = new SmtpService(smtpOptions,
            logger,
            sesService,
            smtpClientWrapperFactoryMock.Object,
            secureSocketOptionsMappingMock.Object,
            emailAuditService,
            exceptionService,
            sesOptions,
            metadataClientFactoryMock.Object,
            metadataTracker);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            metaDataClientMock.Verify(x => x.AddEmailAsync(
                It.Is<EmailMetadata>(m =>
                m.StatusReason.Equals("535") &&
                m.ExtraInfoType.Equals("SmtpResponseMessage") &&
                m.Status.Equals("SmtpSendFailed")
                ), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Failure, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendCustom_Sucess_ShouldCallMetaDataAPIWithSuceededStatusData(
            [Frozen] Mock<ISmtpClientWrapperFactory> smtpClientWrapperFactoryMock,
            [Frozen] Mock<SmtpClientWrapper> mailTransportMock,
            [Frozen] Mock<ISecureSocketOptionsMapping> secureSocketOptionsMappingMock,
            [Frozen] Mock<IMetadataClient> metaDataClientMock,
            [Frozen] Mock<IMetadataClientFactory> metadataClientFactoryMock,
            SecureSocketOptions option,
            TlsOption tlsOption,
            string emailId,
            IOptions<SmtpServiceConfiguration> smtpOptions,
            ILogger<SmtpService> logger,
            IAmazonSimpleEmailServiceV2 sesService,
            IEmailAuditService emailAuditService,
            IExceptionService exceptionService,
            IOptions<AmazonSESConfiguration> sesOptions,
            IMetadataTracker metadataTracker
        )
        {
            // ARRANGE
            const string host = "host";
            const int port = 25;
            const string username = "username";
            const string password = "password";

            var mimeMessage = new MimeMessage();
            mimeMessage.To.Add(MailboxAddress.Parse("david@david.com"));
            smtpClientWrapperFactoryMock.Setup(c => c.CreateSmtpClientWrapper()).Returns(mailTransportMock.Object);
            secureSocketOptionsMappingMock.Setup(x => x.SecureSocketOptionsMapper(tlsOption.Option)).Returns(option);
            mailTransportMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).Returns(Task.FromResult(true));
            mailTransportMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            metaDataClientMock.Setup(x => x.AddEmailAsync(It.IsAny<EmailMetadata>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(""));
            metadataClientFactoryMock.Setup(x => x.CreateMetadataClient()).Returns(metaDataClientMock.Object);

            SmtpService target = new SmtpService(smtpOptions,
            logger,
            sesService,
            smtpClientWrapperFactoryMock.Object,
            secureSocketOptionsMappingMock.Object,
            emailAuditService,
            exceptionService,
            sesOptions,
            metadataClientFactoryMock.Object,
            metadataTracker);

            // ACT
            var result = await target.SendCustomSmtp(mimeMessage, host, port, tlsOption, username, password, emailId);

            // ASSERT
            mailTransportMock.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.AuthenticateAsync(It.IsAny<Encoding>(), It.IsAny<ICredentials>(), It.IsAny<CancellationToken>()), Times.Once);
            mailTransportMock.Verify(x => x.SendAsync(mimeMessage, It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
            mailTransportMock.Verify(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            metaDataClientMock.Verify(x => x.AddEmailAsync(
                It.Is<EmailMetadata>(m =>
                m.StatusReason.Equals("200") &&
                m.ExtraInfoType.Equals("SmtpSendSucceeded") &&
                m.Status.Equals("SmtpSendSucceeded")
                ), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
            Assert.Equal(EdgeType.Custom, result.EdgeType);
        }
    }
}
