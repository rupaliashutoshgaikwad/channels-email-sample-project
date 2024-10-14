using AutoFixture.Xunit2;
using AutoMapper;
using CloudEmail.API.Models;
using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.Mime.Libraries.Models;
using CloudEmail.Mime.Libraries.Services;
using CloudEmail.Mime.Libraries.Services.Interfaces;
using CloudEmail.SampleProject.API.Mappings;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class SendEmailServiceTests
    {
        private static readonly IMapper mapper;

        static SendEmailServiceTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            mapper = config.CreateMapper();
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenCustomEmailRequest_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            TlsOption option,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);

            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(true, "host", 123, "user", "pass", option, new AuthenticationOption { Id = 1 }, null);

            smtpServiceMock.Setup(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object, 
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenCustomReportEmailRequest_SendKerio_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            TlsOption option,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.Report
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);

            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(true, "host", 123, "user", "pass", option, new AuthenticationOption { Id = 1 }, null);

            smtpServiceMock.Setup(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId)).ReturnsAsync(sendEmailResponse);
            smtpServiceMock.Setup(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId), Times.Once);
            smtpServiceMock.Verify(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter), Times.Never);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Kerio, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenCustomProactiveEmailRequest_NoCustomSmtpFound_ReturnsDroppedResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            TlsOption option,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.Proactive
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);

            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter), Times.Never);
            Assert.Equal(SendEmailResponseCode.Dropped, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenCustomEmailRequestWithCert_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            TlsOption option,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            byte[] certificateData
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var authOption = new AuthenticationOption { Id = 2 };

            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(true, "host", 123, "user", "pass", option, authOption, certificateData);

            smtpServiceMock.Setup(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendCustomSmtp(It.IsAny<MimeMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TlsOption>(), It.IsAny<string>(), It.IsAny<string>(), emailId, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>(), EmailType.CallCenter), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenCustomEmailRequestWithNullSettings_AttemptsSes_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object,
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.SES, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenSesEmailRequest_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            TlsOption option,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("Test <test@test.com>");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(false, null, 1, null, null, option);

            blacklistServiceMock.Setup(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()));
            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.SES, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            //blacklistServiceMock.Verify(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()), Times.Once);
            smtpServiceMock.Verify(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.SES, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenKerioEmailRequest_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            MimeService MimeService,
            TlsOption option,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper,
            string sentTimeStamp
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("Test <test@test.com>");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(false, null, 1, null, null, option);

            blacklistServiceMock.Setup(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()));
            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false)).ReturnsAsync(new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.DomainNotVerified });
            smtpServiceMock.Setup(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Kerio, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Kerio, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
        }

        /*
        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_GivenFullyBlacklistedRequest_ReturnsFullyBlaclistedResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            MimeService MimeService,
            TlsOption option,
            int busNo,
            string contactId,
            string testEmailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.Cc.Clear();
            mimeWrapper.Bcc.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = testEmailId
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(false, null, 1, null, null, option);

            blacklistServiceMock
                .Setup(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()))
                .Callback<MimeMessage, string>((mimeMsg, emailId) =>
                {
                    mimeMessage.To.Clear();
                    mimeMessage.To.AddRange(new List<InternetAddress>());
                });
            publishServiceMock.Setup(p => p.PublishSendEmailBlacklist(It.IsAny<string>(), EdgeType.SES)).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, mapperMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            publishServiceMock.Verify(p => p.PublishSendEmailBlacklist(It.IsAny<string>(), EdgeType.SES), Times.Once);
            Assert.Equal(SendEmailResponseCode.FullyBlacklisted, result.SendEmailResponseCode);
        }
        */

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_NotVerifiedDomain_10MBAttachmentSendKerio_ReturnsKerioResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            MimeService MimeService,
            TlsOption option,
            string sentTimeStamp
        )
        {
            // ARRANGE
            var loggingServiceMock = new Mock<ILogger<SendEmailService>>();
            string text = System.IO.File.ReadAllText(@"../../../10MBSendRequestTest.txt");
            SendEmailRequest sendEmailRequest =
                   JsonConvert.DeserializeObject<SendEmailRequest>(text);
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(sendEmailRequest.MimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(false, null, 1, null, null, option);

            blacklistServiceMock
                .Setup(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()))
                .Callback<MimeMessage, string>((mimeMsg, emailId) =>
                { });
            publishServiceMock.Setup(p => p.PublishSendEmailBlacklist(It.IsAny<string>(), EdgeType.SES)).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(false);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(p => p.SendKerio(It.IsAny<MimeMessage>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_VerifiedDomain_10MBAttachmentSendSES_ReturnsSESResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            MimeService MimeService,
            TlsOption option
        )
        {
            // ARRANGE
            var loggingServiceMock = new Mock<ILogger<SendEmailService>>();
            string text = System.IO.File.ReadAllText(@"../../../10MBSendRequestTest.txt");
            SendEmailRequest sendEmailRequest =
                   JsonConvert.DeserializeObject<SendEmailRequest>(text);
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(sendEmailRequest.MimeWrapper);
            var mimeMessage = MimeService.BuildMimeMessageFromMimeWrapper(librariesMimeWrapper);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(false, null, 1, null, null, option);

            blacklistServiceMock
                .Setup(b => b.RemoveBlacklistedRecipients(It.IsAny<MimeMessage>(), It.IsAny<string>()))
                .Callback<MimeMessage, string>((mimeMsg, emailId) =>
                { });

            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), It.IsAny<string>(), false)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailBlacklist(It.IsAny<string>(), EdgeType.SES)).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(p => p.SendSes(It.IsAny<MimeMessage>(), It.IsAny<string>(), false), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_VerifiedDomain_25MBEmail_AttemptsSes_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("testfrom@testfrom.com");
            mailMessage.To.Add(new MailAddress("test@somedomain.com"));

            var imageData = new byte[26214400];

            mailMessage.Attachments.Add(new Attachment(new System.IO.MemoryStream(imageData, false), "test.jpg", "image/jpeg"));
            var mimeMessage = (MimeMessage)mailMessage;


            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.SES, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_VerifiedDomain_45MBEmail_AttemptsSes_ReturnsUnsendableResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("testfrom@testfrom.com");
            mailMessage.To.Add(new MailAddress("test@somedomain.com"));

            var imageData = new byte[47185920];

            mailMessage.Attachments.Add(new Attachment(new System.IO.MemoryStream(imageData, false), "test.jpg", "image/jpeg"));
            var mimeMessage = (MimeMessage)mailMessage;


            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            smtpServiceMock.Setup(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendSes(It.IsAny<MimeMessage>(), emailId, false), Times.Never);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.SES, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Unsendable, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_NotVerifiedDomain_25MBEmail_AttemptsKerio_ReturnsSuccessResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("testfrom@testfrom.com");
            mailMessage.To.Add(new MailAddress("test@somedomain.com"));

            var imageData = new byte[26214400];

            mailMessage.Attachments.Add(new Attachment(new System.IO.MemoryStream(imageData, false), "test.jpg", "image/jpeg"));
            var mimeMessage = (MimeMessage)mailMessage;


            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            smtpServiceMock.Setup(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(false);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId), Times.Once);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Kerio, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(sendEmailResponse.MessageId, result.MessageId);
            Assert.Equal(SendEmailResponseCode.Success, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendRoutedEmail_NotVerifiedDomain_45MBEmail_AttemptsKerio_ReturnsUnsendableResponse(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            mimeWrapper.To.Clear();
            mimeWrapper.From.Clear();
            mimeWrapper.To.Add("test@test.com");
            mimeWrapper.From.Add("testfrom@testfrom.com");

            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter
            };
            var librariesMimeWrapper = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);

            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("testfrom@testfrom.com");
            mailMessage.To.Add(new MailAddress("test@somedomain.com"));

            var imageData = new byte[47185920];

            mailMessage.Attachments.Add(new Attachment(new System.IO.MemoryStream(imageData, false), "test.jpg", "image/jpeg"));
            var mimeMessage = (MimeMessage)mailMessage;


            CloudCustomSmtpSettings cloudCustomSmtpSettings = null;

            smtpServiceMock.Setup(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId)).ReturnsAsync(sendEmailResponse);
            publishServiceMock.Setup(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Custom, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>())).Returns(Task.FromResult(true));
            customSmtpConfigurationServiceMock.Setup(c => c.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(cloudCustomSmtpSettings);
            MimeServiceMock.Setup(x => x.BuildMimeMessageFromMimeWrapper(It.IsAny<LibrariesMimeWrapper>())).Returns(mimeMessage);
            domainVerificationServiceMock.Setup(x => x.IsDomainVerified(It.IsAny<string>())).ReturnsAsync(false);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.SendRoutedEmail(sendEmailRequest, 1);

            // ASSERT
            smtpServiceMock.Verify(s => s.SendKerio(It.IsAny<MimeMessage>(), emailId), Times.Never);
            publishServiceMock.Verify(p => p.PublishSendEmailResults(It.IsAny<SendEmailRequest>(), It.IsAny<string>(), EdgeType.Kerio, It.IsAny<SendEmailResponse>(), It.IsAny<MailAddress>(), It.IsAny<MimeMessage>()), Times.Once);
            Assert.Equal(SendEmailResponseCode.Unsendable, result.SendEmailResponseCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task UpdateDFOExternalAttribute_GivenValidDfoEmailRequest_ShouldCallDfoUpdateMessageAttribute(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            string tenantId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            var sendMessageId = "U3EZ81R4UNU4.VMIUE9SDVOMA@send-email-api";
            sendEmailResponse.MessageId = sendMessageId;
            var updateAttributeReplytoValue = sendMessageId.Split('@')[0];
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter,
                isDFOMail = true,
                DfoChannelId = "email_cxone-email_testpoc@domain.com",
                DfoTenantId = tenantId
            };

            dfoMessageServiceMock.Setup(d => d.UpdateMessageAttribute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.UpdateDFOExternalAttribute(sendEmailRequest, sendEmailResponse);

            // ASSERT
            dfoMessageServiceMock.Verify(d => d.UpdateMessageAttribute(
                It.Is<string>(s => s.ToLower().Equals(sendEmailRequest.DfoChannelId.ToLower())), 
                It.Is<string>(s => s.ToLower().Equals(sendEmailRequest.EmailId.ToLower())), 
                It.Is<string>(s => s.ToLower().Equals(updateAttributeReplytoValue.ToLower())),
                It.Is<string>(s => s.ToLower().Equals(tenantId.ToLower()))), Times.Once);
            Assert.True(result);
        }

        [Theory]
        [AutoMoqData]
        public async Task UpdateDFOExternalAttribute_GivenNoChannelIdDfoEmailRequest_ShouldNotCallDfoUpdateMessageAttribute(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            [Frozen] Mock<IBlacklistService> blacklistServiceMock,
            [Frozen] Mock<IPublishResultsService> publishServiceMock,
            [Frozen] Mock<ICustomSmtpConfigurationService> customSmtpConfigurationServiceMock,
            [Frozen] Mock<IMimeService> MimeServiceMock,
            [Frozen] Mock<ILogger<SendEmailService>> loggingServiceMock,
            [Frozen] Mock<IDomainVerificationService> domainVerificationServiceMock,
            [Frozen] Mock<IDFOMessageService> dfoMessageServiceMock,
            [Frozen] Mock<IMapper> mapperMock,
            [Frozen] Mock<IFeatureToggleService> featureToggleServiceMock,
            SendEmailResponse sendEmailResponse,
            int busNo,
            string contactId,
            string emailId,
            MimeWrapper mimeWrapper
        )
        {
            // ARRANGE
            var sendEmailRequest = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper,
                EmailId = emailId,
                EmailType = EmailType.CallCenter,
                isDFOMail = true,
                DfoChannelId = null
            };

            var sendEmailService = new SendEmailService(smtpServiceMock.Object, blacklistServiceMock.Object, publishServiceMock.Object,
                customSmtpConfigurationServiceMock.Object, MimeServiceMock.Object, loggingServiceMock.Object, domainVerificationServiceMock.Object, 
                mapperMock.Object, featureToggleServiceMock.Object, dfoMessageServiceMock.Object);

            // ACT
            var result = await sendEmailService.UpdateDFOExternalAttribute(sendEmailRequest, sendEmailResponse);

            // ASSERT
            dfoMessageServiceMock.Verify(d => d.UpdateMessageAttribute(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.False(result);
        }
    }
}
