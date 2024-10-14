using System.Diagnostics.CodeAnalysis;
using System.IO;
using MimeKit;

namespace CloudEmail.SampleProject.API.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class MimeMessageExtensions
    {
        public static MemoryStream GetStream(this MimeMessage message)
        {
            var stream = new MemoryStream();
            message.WriteTo(stream);
            return stream;
        }
    }
}
