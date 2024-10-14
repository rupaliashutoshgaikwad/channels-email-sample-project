using CloudEmail.API.Models.Requests;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using FluentAssertions;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class SerializationServiceTests
    {
        [Theory]
        [AutoMoqData]
        public void Serialize_SendEmailRequest_Json(
            SerializationService target,
            SendEmailRequest obj
        )
        {
            // ACT
            var json = target.SerializeToJsonString(obj);
            json.Should().BeOfType(typeof(string));
        }

        [Theory]
        [AutoMoqData]
        public void Deserialize_StreamIntoSendEmailRequest(
            SerializationService target,
            SendEmailRequest obj
        )
        {
            var json = target.SerializeToJsonString(obj);
            var ser = Encoding.ASCII.GetBytes(json);
            using (var stream = new MemoryStream(ser))
            {
                var originalObject = target.DeserializeStreamIntoObject<SendEmailRequest>(stream);
                originalObject.Should().BeOfType(typeof(SendEmailRequest));
                originalObject.Should().BeEquivalentTo(obj);
            }
        }
    }
}
