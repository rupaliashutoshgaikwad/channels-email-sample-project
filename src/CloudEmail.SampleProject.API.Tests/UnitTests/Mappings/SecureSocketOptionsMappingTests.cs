using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Mappings;
using FluentAssertions;
using MailKit.Security;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Mappings
{
    public class SecureSocketOptionsMappingTests
    {
        [Theory]
        [AutoMoqData]
        public void SecureSocketOptionsMapper_GivenNone_ReturnsSecureSocketOptionNone(
            SecureSocketOptionsMapping secureSocketOptionsMapping
        )
        {
            // Arrange
            const string tlsOptionName = "None";

            // Act
            var result = secureSocketOptionsMapping.SecureSocketOptionsMapper(tlsOptionName);

            // Assert
            result.Should().Be(SecureSocketOptions.None);
        }

        [Theory]
        [AutoMoqData]
        public void SecureSocketOptionsMapper_GivenNone_ReturnsSecureSocketOptionSslOnConnect(
            SecureSocketOptionsMapping secureSocketOptionsMapping
        )
        {
            // Arrange
            const string tlsOptionName = "Require TLS";

            // Act
            var result = secureSocketOptionsMapping.SecureSocketOptionsMapper(tlsOptionName);

            // Assert
            result.Should().Be(SecureSocketOptions.SslOnConnect);
        }

        [Theory]
        [AutoMoqData]
        public void SecureSocketOptionsMapper_GivenNone_ReturnsSecureSocketOptionStartTlsWhenAvailable(
            SecureSocketOptionsMapping secureSocketOptionsMapping
        )
        {
            // Arrange
            const string tlsOptionName = "Opportunistic TLS";

            // Act
            var result = secureSocketOptionsMapping.SecureSocketOptionsMapper(tlsOptionName);

            // Assert
            result.Should().Be(SecureSocketOptions.StartTlsWhenAvailable);
        }

        [Theory]
        [AutoMoqData]
        public void SecureSocketOptionsMapper_GivenDefault_ReturnsSecureSocketOptionStartTlsWhenAvailable(
            SecureSocketOptionsMapping secureSocketOptionsMapping,
            string tlsOptionName
        )
        {
            // Act
            var result = secureSocketOptionsMapping.SecureSocketOptionsMapper(tlsOptionName);

            // Assert
            result.Should().Be(SecureSocketOptions.StartTlsWhenAvailable);
        }
    }
}