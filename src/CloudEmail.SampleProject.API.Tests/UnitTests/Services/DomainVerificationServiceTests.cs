using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using AutoFixture.Xunit2;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class DomainVerificationServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task IsDomainVerified_GivenDomainInCache_ReturnsTrue(
            [Frozen] Mock<IAmazonSimpleEmailService> amazonSimpleEmailServiceMock,
            [Frozen] Mock<ILogger<DomainVerificationService>> loggerMock,
            [Frozen] Mock<IConfiguration> configurationMock
        )
        {
            // ARRANGE
            var domain = "test.com";
            var memoryCacheOptions = new MemoryCacheOptions();
            var memCache = new MemoryCache(memoryCacheOptions);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromSeconds(60));
            memCache.Set(domain, true, cacheEntryOptions);
            var domainVerificationService = new DomainVerificationService(amazonSimpleEmailServiceMock.Object, loggerMock.Object, memCache,
                configurationMock.Object);

            // ACT
            var result = await domainVerificationService.IsDomainVerified(domain);

            // ASSERT
            Assert.True(result);
        }

        [Theory]
        [AutoMoqData]
        public async Task IsDomainVerified_GivenDomainInCache_ReturnsFalse(
            [Frozen] Mock<IAmazonSimpleEmailService> amazonSimpleEmailServiceMock,
            [Frozen] Mock<ILogger<DomainVerificationService>> loggerMock,
            [Frozen] Mock<IConfiguration> configurationMock
        )
        {
            // ARRANGE
            var domain = "test.com";
            var memoryCacheOptions = new MemoryCacheOptions();
            var memCache = new MemoryCache(memoryCacheOptions);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromSeconds(60));
            memCache.Set(domain, false, cacheEntryOptions);
            var domainVerificationService = new DomainVerificationService(amazonSimpleEmailServiceMock.Object, loggerMock.Object, memCache,
                configurationMock.Object);

            // ACT
            var result = await domainVerificationService.IsDomainVerified(domain);

            // ASSERT
            Assert.False(result);
        }

        [Theory]
        [AutoMoqData]
        public async Task IsDomainVerified_GivenDomainNotInCache_ReturnsTrue(
            [Frozen] Mock<IAmazonSimpleEmailService> amazonSimpleEmailServiceMock,
            [Frozen] Mock<ILogger<DomainVerificationService>> loggerMock,
            [Frozen] Mock<IMemoryCache> memoryCacheMock,
            [Frozen] Mock<IConfiguration> configurationMock
        )
        {
            // ARRANGE
            var domain = "test.com";
            var awsResponse = new GetIdentityVerificationAttributesResponse();
            var identityAttribute = new IdentityVerificationAttributes
            {
                VerificationStatus = VerificationStatus.Success
            };
            awsResponse.VerificationAttributes.Add(domain, identityAttribute);         

            amazonSimpleEmailServiceMock
                .Setup(x => x.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None))
                .ReturnsAsync(awsResponse);

            var domainVerificationService = new DomainVerificationService(amazonSimpleEmailServiceMock.Object, loggerMock.Object, memoryCacheMock.Object,
                configurationMock.Object);

            // ACT
            var result = await domainVerificationService.IsDomainVerified(domain);

            // ASSERT
            amazonSimpleEmailServiceMock.Verify(
                s => s.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None),
                Times.Once);
            Assert.True(result);
        }

        [Theory]
        [AutoMoqData]
        public async Task IsDomainVerified_GivenDomainNotInCache_ReturnsFalse(
            [Frozen] Mock<IAmazonSimpleEmailService> amazonSimpleEmailServiceMock,
            [Frozen] Mock<ILogger<DomainVerificationService>> loggerMock,
            [Frozen] Mock<IMemoryCache> memoryCacheMock,
            [Frozen] Mock<IConfiguration> configurationMock
        )
        {
            // ARRANGE
            var domain = "test.com";
            var awsResponse = new GetIdentityVerificationAttributesResponse();
            var identityAttribute = new IdentityVerificationAttributes
            {
                VerificationStatus = VerificationStatus.Failed
            };
            awsResponse.VerificationAttributes.Add(domain, identityAttribute);

            amazonSimpleEmailServiceMock
                .Setup(x => x.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None))
                .ReturnsAsync(awsResponse);

            var domainVerificationService = new DomainVerificationService(amazonSimpleEmailServiceMock.Object, loggerMock.Object, memoryCacheMock.Object,
                configurationMock.Object);

            // ACT
            var result = await domainVerificationService.IsDomainVerified(domain);

            // ASSERT
            amazonSimpleEmailServiceMock.Verify(
                s => s.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None),
                Times.Once);
            Assert.False(result);
        }

        [Theory]
        [AutoMoqData]
        public async Task IsDomainVerified_GivenDomainNotInCacheAndDoesNotExist_ReturnsFalse(
            [Frozen] Mock<IAmazonSimpleEmailService> amazonSimpleEmailServiceMock,
            [Frozen] Mock<ILogger<DomainVerificationService>> loggerMock,
            [Frozen] Mock<IMemoryCache> memoryCacheMock,
            [Frozen] Mock<IConfiguration> configurationMock
        )
        {
            // ARRANGE
            var domain = "test.com";
            var awsResponse = new GetIdentityVerificationAttributesResponse();
            var identityAttribute = new IdentityVerificationAttributes
            {
                VerificationStatus = VerificationStatus.Failed
            };

            amazonSimpleEmailServiceMock
                .Setup(x => x.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None))
                .ReturnsAsync(awsResponse);

            var domainVerificationService = new DomainVerificationService(amazonSimpleEmailServiceMock.Object, loggerMock.Object, memoryCacheMock.Object,
                configurationMock.Object);

            // ACT
            var result = await domainVerificationService.IsDomainVerified(domain);

            // ASSERT
            amazonSimpleEmailServiceMock.Verify(
                s => s.GetIdentityVerificationAttributesAsync(It.IsAny<GetIdentityVerificationAttributesRequest>(), CancellationToken.None),
                Times.Once);
            Assert.False(result);
        }
    }
}
