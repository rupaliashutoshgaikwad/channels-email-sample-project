using Amazon.CloudWatch.Model;
using AutoFixture.Xunit2;
using CloudEmail.API.Clients.Interfaces;
using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Clients;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Clients
{
    [ExcludeFromCodeCoverage]
    public class PublishResultsClientTests
    {
        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailSuccess_GivenValidInput_VerifyAllMethodsCalled(
            [Frozen] Mock<ICloudWatchClient> cloudWatchClientMock,
            string emailId, 
            string messageId, 
            string businessUnit, 
            EdgeType edgeType, 
            string from, 
            string toAddresses,
            PublishResultsClient publishResultsClient
        )
        {
            // Arrange
            cloudWatchClientMock.Setup(x => x.PublishAsync(edgeType, OutboundOutcome.Success)).ReturnsAsync(It.IsAny<PutMetricDataResponse>());
            cloudWatchClientMock.Setup(x => x.PublishAsync(edgeType, OutboundOutcome.Success, businessUnit)).ReturnsAsync(It.IsAny<PutMetricDataResponse>());

            // Act
            await publishResultsClient.PublishSendEmailSuccess(emailId, messageId, businessUnit, edgeType, from, toAddresses);

            // Assert
            cloudWatchClientMock.Verify(x => x.PublishAsync(edgeType, OutboundOutcome.Success), Times.Once);
            cloudWatchClientMock.Verify(x => x.PublishAsync(edgeType, OutboundOutcome.Success, businessUnit), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailBlacklist_GivenValidInput_VerifyAllMethodsCalled(
            [Frozen] Mock<ICloudWatchClient> cloudWatchClientMock,
            string businessUnit,
            EdgeType edgeType,
            PublishResultsClient publishResultsClient
        )
        {
            // Arrange
            cloudWatchClientMock.Setup(x => x.PublishAsync(edgeType, OutboundOutcome.Failure))
                .ReturnsAsync(It.IsAny<PutMetricDataResponse>());
            cloudWatchClientMock.Setup(x => x.PublishAsync(edgeType, OutboundOutcome.Failure, businessUnit))
                .ReturnsAsync(It.IsAny<PutMetricDataResponse>());

            // Act
            await publishResultsClient.PublishSendEmailFailure(businessUnit, edgeType);

            // Assert
            cloudWatchClientMock.Verify(x => x.PublishAsync(edgeType, OutboundOutcome.Failure), Times.Once);
            cloudWatchClientMock.Verify(x => x.PublishAsync(edgeType, OutboundOutcome.Failure, businessUnit),
                Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task PublishSendEmailResults_GivenSuccessfulEmailSend_PublishMetrics(
            [Frozen] Mock<ICloudWatchClient> cloudwatchClientMock,
            PublishResultsClient target,
            string businessUnit,
            string domain
        )
        {
            // ARRANGE
            cloudwatchClientMock.Setup(x => x.PublishAsync()).ReturnsAsync(It.IsAny<PutMetricDataResponse>());
            cloudwatchClientMock.Setup(x => x.PublishAsync(It.Is<string>(y => y.Equals(businessUnit)), It.Is<string>(z => z.Equals(domain)))).ReturnsAsync(It.IsAny<PutMetricDataResponse>());

            //// ACT
            await target.PublishSendEmailDropped(domain, businessUnit);

            // ASSERT
            cloudwatchClientMock.Verify(x => x.PublishAsync(), Times.Once);
            cloudwatchClientMock.Verify(x => x.PublishAsync(businessUnit, domain), Times.Once);
        }
    }
}
