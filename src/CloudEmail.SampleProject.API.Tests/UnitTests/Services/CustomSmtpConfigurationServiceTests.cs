using AutoFixture.Xunit2;
using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.Management.API.Models;
using CloudEmail.SampleProject.API.Services;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class CustomSmtpConfigurationServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task GetCustomSmtpConfiguration_GivenValidBusinessUnit_ReturnsCloudCustomSmtpSettings(
            [Frozen] Mock<ISmtpServerClient> smtpServerClientMock,
            CustomSmtpConfigurationService customSmtpConfigurationService,
            SmtpServerDetail smtpServerDetail
        )
        {
            var smtpServer = smtpServerDetail.SmtpServer;
            smtpServer.AuthenticationOptionId = 1;

            // ARRANGE
            smtpServerClientMock.Setup(x => x.GetSmtpServerByBusinessUnitAndDomain(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(smtpServerDetail);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(smtpServer.Enabled, smtpServer.Host,
                smtpServer.Port, smtpServer.Username, smtpServer.Password, smtpServer.TlsOption);

            // ACT
            var result = await customSmtpConfigurationService.GetCustomSmtpConfiguration(new Random().Next(), It.IsAny<string>());

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(cloudCustomSmtpSettings.Enabled, result.Enabled);
            Assert.Equal(cloudCustomSmtpSettings.Host, result.Host);
            Assert.Equal(cloudCustomSmtpSettings.Port, result.Port);
            Assert.Equal(cloudCustomSmtpSettings.Username, result.Username);
            Assert.Equal(cloudCustomSmtpSettings.Password, result.Password);
            Assert.Equal(cloudCustomSmtpSettings.TlsOption, result.TlsOption);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetCustomSmtpConfiguration_GivenValidBusinessUnit_ReturnsCloudCustomSmtpSettings_WithCert(
            //[Frozen] Mock<ISmtpServerBusinessUnitRepository> smtpServerBusinessUnitRepositoryMock,
            //[Frozen] Mock<ISmtpServerCertificateRepository> smtpServerCertificateRepositoryMock,
            [Frozen] Mock<ISmtpServerClient> smtpServerClientMock,
            CustomSmtpConfigurationService customSmtpConfigurationService,
            //SmtpServer smtpServer,
            //SmtpServerCertificate smtpServerCertificate,
            SmtpServerDetail smtpServerDetail
        )
        {
            var smtpServer = smtpServerDetail.SmtpServer;
            var smtpServerCertificate = smtpServerDetail.SmtpServerCertificate;
            smtpServer.AuthenticationOptionId = 2;

            // ARRANGE
            //smtpServerBusinessUnitRepositoryMock.Setup(x => x.GetSmtpServerByBusinessUnit(It.IsAny<int>())).ReturnsAsync(smtpServer);
            //smtpServerCertificateRepositoryMock.Setup(x => x.GetSmtpServerCertificateByServerId(It.IsAny<int>())).ReturnsAsync(smtpServerCertificate);
            smtpServerClientMock.Setup(x => x.GetSmtpServerByBusinessUnitAndDomain(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(smtpServerDetail);
            var cloudCustomSmtpSettings = new CloudCustomSmtpSettings(smtpServer.Enabled, smtpServer.Host,
                smtpServer.Port, smtpServer.Username, smtpServer.Password, smtpServer.TlsOption, smtpServer.AuthenticationOption, smtpServerCertificate.CertificateData);

            // ACT
            var result = await customSmtpConfigurationService.GetCustomSmtpConfiguration(It.IsAny<int>(), It.IsAny<string>());

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(cloudCustomSmtpSettings.Enabled, result.Enabled);
            Assert.Equal(cloudCustomSmtpSettings.Host, result.Host);
            Assert.Equal(cloudCustomSmtpSettings.Port, result.Port);
            Assert.Equal(cloudCustomSmtpSettings.Username, result.Username);
            Assert.Equal(cloudCustomSmtpSettings.Password, result.Password);
            Assert.Equal(cloudCustomSmtpSettings.TlsOption, result.TlsOption);
            Assert.Equal(cloudCustomSmtpSettings.CertificateData, result.CertificateData);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetCustomSmtpConfiguration_GivenInvalidBusinessUnit_ReturnsNull(
            CustomSmtpConfigurationService customSmtpConfigurationService
        )
        {
            // ACT
            var result = await customSmtpConfigurationService.GetCustomSmtpConfiguration(new Random().Next(), It.IsAny<string>());

            // ASSERT
            Assert.Null(result);
        }
    }
}
