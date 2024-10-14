using CloudEmail.SampleProject.API.Mappings.Interfaces;
using MailKit.Security;

namespace CloudEmail.SampleProject.API.Mappings
{
    public class SecureSocketOptionsMapping : ISecureSocketOptionsMapping
    {
        public SecureSocketOptions SecureSocketOptionsMapper(string tlsOptionName)
        {
            switch (tlsOptionName.ToLowerInvariant())
            {
                case "none":
                    return SecureSocketOptions.None;
                case "require tls":
                    return SecureSocketOptions.SslOnConnect;
                case "opportunistic tls":
                default:
                    return SecureSocketOptions.StartTlsWhenAvailable;
            }
        }
    }
}
