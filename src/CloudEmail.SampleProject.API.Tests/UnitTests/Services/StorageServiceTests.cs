using Amazon.S3;
using Amazon.S3.Model;
using AutoFixture.Xunit2;
using CloudEmail.API.Models.Requests;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.SampleProject.API.Services.Interface;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class StorageServiceTests
    {
        [Theory]
        [AutoMoqData]
        public async Task GetObjectFromStorage_ValidResponse_ReturnsObject(
            [Frozen] Mock<IAmazonS3> clientMock,
            [Frozen] Mock<ISerializationService> serializationServiceMock,
            StorageService target,
            string storageKey,
            SendEmailRequest request
        )
        {
            //ARRANGE
            var response = new GetObjectResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            };
            clientMock.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>())).ReturnsAsync(response);
            serializationServiceMock.Setup(x => x.DeserializeStreamIntoObject<SendEmailRequest>(It.IsAny<Stream>())).Returns(request);

            //ACT
            var result = await target.GetObjectFromStorage<SendEmailRequest>(storageKey);

            //ASSERT
            clientMock.Verify(x => x.GetObjectAsync(It.Is<GetObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>()), Times.Once);
            serializationServiceMock.Verify(x => x.DeserializeStreamIntoObject<SendEmailRequest>(It.IsAny<Stream>()), Times.Once);
            result.Should().Be(request);
        }

        [Theory]
        [AutoMoqData]
        public async Task GetObjectFromStorage_InvalidResponse_ThrowsException(
            [Frozen] Mock<IAmazonS3> clientMock,
            StorageService target,
            string storageKey
        )
        {
            //ARRANGE
            var response = new GetObjectResponse
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
            clientMock.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            //ACT/ASSERT
            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() => target.GetObjectFromStorage<SendEmailRequest>(storageKey));
            exception.Message.Should().Contain(storageKey);
            clientMock.Verify(x => x.GetObjectAsync(It.Is<GetObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqStreamData]
        public async Task PutObjectToStorage_ValidResponse(
            [Frozen] Mock<IAmazonS3> clientMock,
            [Frozen] Mock<ISerializationService> serializationServiceMock,
            StorageService target,
            SendEmailRequest request,
            string storageKey
        )
        {
            //ARRANGE
            var json = JsonConvert.SerializeObject(request);
            var response = new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            };
            serializationServiceMock.Setup(x => x.SerializeToJsonString(request)).Returns(json);
            clientMock.Setup(x => x.PutObjectAsync(It.Is<PutObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            //ACT
            await target.PutObjectToStorage(request, storageKey);

            //ASSERT
            serializationServiceMock.Verify(x => x.SerializeToJsonString(request), Times.Once);
            clientMock.Verify(x => x.PutObjectAsync(It.Is<PutObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqStreamData]
        public async Task PutObjectToStorage_InvalidResponse_ThrowsException(
            [Frozen] Mock<IAmazonS3> clientMock,
            [Frozen] Mock<ISerializationService> serializationServiceMock,
            StorageService target,
            SendEmailRequest request,
            string storageKey
        )
        {
            //ARRANGE
            var json = JsonConvert.SerializeObject(request);
            var response = new PutObjectResponse
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
            serializationServiceMock.Setup(x => x.SerializeToJsonString(request)).Returns(json);
            clientMock.Setup(x => x.PutObjectAsync(It.Is<PutObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            //ACT/ASSERT
            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() => target.PutObjectToStorage(request, storageKey));
            exception.Message.Should().Contain(storageKey);
            serializationServiceMock.Verify(x => x.SerializeToJsonString(request), Times.Once);
            clientMock.Verify(x => x.PutObjectAsync(It.Is<PutObjectRequest>(a => a.Key == storageKey), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
