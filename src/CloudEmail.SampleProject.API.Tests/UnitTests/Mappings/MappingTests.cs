using AutoMapper;
using CloudEmail.API.Models;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.Mime.Libraries.Models;
using CloudEmail.SampleProject.API.Mappings;
using FluentAssertions;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Mappings
{
    [ExcludeFromCodeCoverage]
    public class MappingTests
    {
        private static readonly IMapper mapper;

        static MappingTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            mapper = config.CreateMapper();
        }

        [Fact]
        public void AutoMapper_Configuration_IsValid() => mapper.ConfigurationProvider.AssertConfigurationIsValid();

        [AutoMoqData]
        [Theory]
        public void AutoMapper_ConvertFromMimeWrapperToLibrariesMimeWrapper_IsValid(
            MimeWrapper mimeWrapper)
        {
            var mapper = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); }).CreateMapper();

            var results = mapper.Map<LibrariesMimeWrapper>(mimeWrapper);

            mapper.ConfigurationProvider.AssertConfigurationIsValid();
            results.Attachments.Count.Should().Be(mimeWrapper.Attachments.Count);
            for (int i = 0; i < results.Attachments.Count; i++)
            {
                results.Attachments[i].Name.Should().Be(mimeWrapper.Attachments[i].Name);
                results.Attachments[i].Data.Should().BeEquivalentTo(mimeWrapper.Attachments[i].Data);
            }
            results.Bcc.Should().BeEquivalentTo(mimeWrapper.Bcc);
            results.To.Should().BeEquivalentTo(mimeWrapper.To);
            results.From.Should().BeEquivalentTo(mimeWrapper.From);
            results.Cc.Should().BeEquivalentTo(mimeWrapper.Cc);
            results.Date.Should().Be(mimeWrapper.Date);
            results.Subject.Should().Be(mimeWrapper.Subject);
            results.TextBody.Should().Be(mimeWrapper.TextBody);
            results.HtmlBody.Should().Be(mimeWrapper.HtmlBody);
        }
    }
}
