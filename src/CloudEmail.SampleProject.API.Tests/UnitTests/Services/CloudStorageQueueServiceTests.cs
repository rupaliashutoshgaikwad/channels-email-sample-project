using Amazon.SQS;
using Amazon.SQS.Model;
using AutoFixture.Xunit2;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using FluentAssertions;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class CloudStorageQueueServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task QueueEmail_GivenEmailId_PutsOnQueue(
           [Frozen] Mock<IAmazonSQS> sqsClientMock,
           string emailId,
           string sentTimeStamp,
           CloudStorageQueueService target
       )
        {
            // ARRANGE
            sqsClientMock.Setup(x => x.SendMessageAsync(It.Is<SendMessageRequest>(y => y.MessageBody.Equals(emailId)), It.IsAny<CancellationToken>())).ReturnsAsync(It.IsAny<SendMessageResponse>());

            // ACT
            var response = await target.PutToQueue(emailId, sentTimeStamp);

            // ASSERT
            response.Should().Be(true);
        }

        [Theory]
        [AutoMoqData]
        public async Task QueueEmail_GivenEmailId_ThrowsError(
            [Frozen] Mock<IAmazonSQS> sqsClientMock,
            string emailId,
            string sentTimeStamp,
            string errorMessage,
            CloudStorageQueueService target
        )
        {
            // ARRANGE
            sqsClientMock.Setup(x => x.SendMessageAsync(It.Is<SendMessageRequest>(y => y.MessageBody.Equals(emailId)), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception(errorMessage));

            // ACT
            var response = await target.PutToQueue(emailId, sentTimeStamp);

            // ASSERT
            response.Should().Be(false);
        }
    }
}
