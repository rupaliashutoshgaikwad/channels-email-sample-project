using Api.Model.Requests.Messages;
using AutoFixture.Xunit2;
using Channels.DFO.Api.Client.Services.Interfaces;
using Channels.UH.Token.Services.Interfaces;
using Channels.UH.Token.Services.Model;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

    public class DFOMessageServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task UpdateMessageAttributes_ValidMessage_ReturnTrue(
            [Frozen] Mock<IMessageService> messageServiceMock,
            [Frozen] Mock<IServiceTokenService> serviceTokenServiceMock,
            [Frozen] Mock<IConfiguration> configurationMock,
            string tenantId
        )
        {
            // ARRANGE
            string channelId = "email_cxone-email_testpoc@domain.com";
            string messageIdOnExternalPlatform = Guid.NewGuid().ToString();
            string sendMessageId = Guid.NewGuid().ToString();

            messageServiceMock.Setup(obj => obj.UpdateMessageExternalAttribute(It.IsAny<PatchMessageExternalAttributesRequest>())).ReturnsAsync(true);
            serviceTokenServiceMock.Setup(obj => obj.GetServiceToken(It.IsAny<Credentials>())).ReturnsAsync(new ServiceToken());

            var dfoMessageService = new DFOMessageService(configurationMock.Object, messageServiceMock.Object, serviceTokenServiceMock.Object);

            // ACT
            var result = await dfoMessageService.UpdateMessageAttribute(channelId, messageIdOnExternalPlatform, sendMessageId, tenantId);

            // ASSERT   
            Assert.True(result);
            messageServiceMock.Verify(d => d.UpdateMessageExternalAttribute(
                It.Is<PatchMessageExternalAttributesRequest>(
                    s => s.channelId.Equals(channelId) && 
                    s.messageIdOnExternalPlatform.Equals(messageIdOnExternalPlatform) &&
                    s.ExternalAttribute.ExternalAttributes["in-reply-to"].ToLower() == sendMessageId.ToLower())
            ), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task UpdateMessageAttributes_InValidMessage_ReturnFalse(
            [Frozen] Mock<IMessageService> messageServiceMock,
            [Frozen] Mock<IServiceTokenService> serviceTokenServiceMock,
            [Frozen] Mock<IConfiguration> configurationMock,
            string tenantId
        )
        {
            // ARRANGE
            string channelId = "email_cxone-email_testpoc@domain.com";
            string messageIdOnExternalPlatform = Guid.NewGuid().ToString();
            string sendMessageId = Guid.NewGuid().ToString();

            messageServiceMock.Setup(obj => obj.UpdateMessageExternalAttribute(It.IsAny<PatchMessageExternalAttributesRequest>())).ReturnsAsync(false);
            serviceTokenServiceMock.Setup(obj => obj.GetServiceToken(It.IsAny<Credentials>())).ReturnsAsync(new ServiceToken());

            var dfoMessageService = new DFOMessageService(configurationMock.Object, messageServiceMock.Object, serviceTokenServiceMock.Object);

            // ACT
            var result = await dfoMessageService.UpdateMessageAttribute(channelId, messageIdOnExternalPlatform, sendMessageId, tenantId);

            // ASSERT   
            Assert.False(result);
            messageServiceMock.Verify(d => d.UpdateMessageExternalAttribute(
                It.Is<PatchMessageExternalAttributesRequest>(
                    s => s.channelId.Equals(channelId) &&
                    s.messageIdOnExternalPlatform.Equals(messageIdOnExternalPlatform) &&
                    s.ExternalAttribute.ExternalAttributes["in-reply-to"].ToLower() == sendMessageId.ToLower())
            ), Times.Once);
        }
    }
}
