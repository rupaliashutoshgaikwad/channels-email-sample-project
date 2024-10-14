using MailKit.Security;

namespace CloudEmail.SampleProject.API.Mappings.Interfaces
{
    public interface ISecureSocketOptionsMapping
    {
        SecureSocketOptions SecureSocketOptionsMapper(string tlsOptionName);
    }
}
