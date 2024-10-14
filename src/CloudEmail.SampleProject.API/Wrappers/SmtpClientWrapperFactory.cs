using CloudEmail.SampleProject.API.Wrappers.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace CloudEmail.SampleProject.API.Wrappers
{
    [ExcludeFromCodeCoverage]
    public class SmtpClientWrapperFactory : ISmtpClientWrapperFactory
    {
        public SmtpClientWrapperFactory() { }

        public SmtpClientWrapper CreateSmtpClientWrapper()
        {
            return new SmtpClientWrapper();
        }
    }
}
