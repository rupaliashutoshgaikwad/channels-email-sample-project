using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Moq;
using CloudEmail.SampleProject.API.Services.Interface;
using CloudEmail.API.Clients.Interfaces;
using Amazon.CloudWatch.Model;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.API.Models.Requests;
using CloudEmail.Common;
using Amazon.S3;
using FluentAssertions;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class EmailStorageServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task PutEmailToUnsendables_ValidRequest_PutsRequestInStorage(
            [Frozen] Mock<IStorageService> storageServiceMock,
            [Frozen] Mock<IEmailAuditService> auditServiceMock,
            EmailStorageService target,
            SendEmailRequest request
        )
        {
            //ARRANGE
            storageServiceMock.Setup(x => x.PutObjectToStorage(request, It.Is<string>(str => str.Contains(request.EmailId)))).Returns(Task.CompletedTask);

            //ACT
            await target.PutEmailToUnsendables(request);

            //ASSERT            
            storageServiceMock.Verify(x => x.PutObjectToStorage(request, It.Is<string>(str => str.Contains(request.EmailId))), Times.Once);
            auditServiceMock.Verify(x => x.LogPutEmailToUnsendablesSuccess(It.Is<string>(str => str.Contains(request.EmailId)), It.Is<int>(val => val == request.BusinessUnit), It.Is<string>(str => str.Contains(request.ContactId))), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PutEmailToUnsendables_InvalidRequest_FailsToPutRequestInStorage(
            [Frozen] Mock<IStorageService> storageServiceMock,
            [Frozen] Mock<ICloudWatchClient> metricsPublisherMock,
            PutMetricDataResponse putMetricDataResponse,
            EmailStorageService target,
            SendEmailRequest request
        )
        {
            //ARRANGE
            metricsPublisherMock.Setup(x => x.PublishAsync(It.IsAny<int>(), It.IsAny<StorageOutcome>())).ReturnsAsync(putMetricDataResponse);
            storageServiceMock.Setup(x => x.PutObjectToStorage(request, It.Is<string>(str => str.Contains(request.EmailId)))).Throws<Exception>();

            //ACT
            var response = await Assert.ThrowsAsync<AmazonS3Exception>(() => target.PutEmailToUnsendables(request));

            //ASSERT
            response.Message.Should().NotBeNullOrEmpty();
            storageServiceMock.Verify(x => x.PutObjectToStorage(request, It.Is<string>(str => str.Contains(request.EmailId))), Times.Once);
            metricsPublisherMock.Verify(x => x.PublishAsync(It.IsAny<int>(), It.IsAny<StorageOutcome>()), Times.Once);
            metricsPublisherMock.Verify(x => x.PublishAsync(It.IsAny<StorageAction>(), It.IsAny<StorageOutcome>()), Times.Once);
        }
    }
}
