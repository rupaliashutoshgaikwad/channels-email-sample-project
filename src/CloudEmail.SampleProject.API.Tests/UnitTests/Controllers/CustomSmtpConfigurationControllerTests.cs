using AutoFixture.Xunit2;
using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Controllers;
using CloudEmail.SampleProject.API.Models.Requests;
using CloudEmail.SampleProject.API.Services.Interface;
using FluentAssertions;
using MimeKit;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Controllers
{
    [ExcludeFromCodeCoverage]
    public class CustomSmtpConfigurationControllerTests
    {
        [Theory]
        [AutoMoqDataOmitProperties]
        public async Task GivenValidSmtpConfiguration_ReturnSuccessAndVerificationToken(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            CustomSmtpConfigurationController customSmtpConfigurationController,
            TestCustomSmtpConfigurationRequest request
        )
        {
            // ARRANGE
            smtpServiceMock.Setup(x =>
                    x.SendCustomSmtp(
                        It.Is<MimeMessage>(y => y.From.Mailboxes.First().Address.Equals(request.FromAddress) && y.To.Mailboxes.First().Address.Equals(request.ToAddress)),
                        request.Host,
                        request.Port,
                        request.TlsOption,
                        request.Username,
                        request.Password,
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        EmailType.CallCenter))
                .ReturnsAsync(new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Success });

            // ACT
            var actionResponse = await customSmtpConfigurationController.SendTestEmail(request);

            // ASSERT
            actionResponse.Should().NotBeNull();

            var response = actionResponse.Value;
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeTrue();
            response.ErrorMessage.Should().BeNullOrEmpty();
        }

        [Theory]
        [AutoMoqDataOmitProperties]
        public async Task GivenInvalidSmtpConfiguration_ReturnFailureAndErrorMessage(
            [Frozen] Mock<ISmtpService> smtpServiceMock,
            CustomSmtpConfigurationController customSmtpConfigurationController,
            TestCustomSmtpConfigurationRequest request,
            string errorMessage
        )
        {
            // ARRANGE
            smtpServiceMock.Setup(x =>
                    x.SendCustomSmtp(
                        It.Is<MimeMessage>(y => y.From.Mailboxes.First().Address.Equals(request.FromAddress) && y.To.Mailboxes.First().Address.Equals(request.ToAddress)),
                        request.Host,
                        request.Port,
                        request.TlsOption,
                        request.Username,
                        request.Password,
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<byte[]>(),
                        It.IsAny<int>(),
                        EmailType.CallCenter))
                .ReturnsAsync(new SendEmailResponse { SendEmailResponseCode = SendEmailResponseCode.Failure, ErrorMessage = errorMessage});

            // ACT
            var actionResponse = await customSmtpConfigurationController.SendTestEmail(request);

            // ASSERT
            actionResponse.Should().NotBeNull();

            var response = actionResponse.Value;
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeFalse();
            response.ErrorMessage.Should().Be(errorMessage);
        }
    }
}