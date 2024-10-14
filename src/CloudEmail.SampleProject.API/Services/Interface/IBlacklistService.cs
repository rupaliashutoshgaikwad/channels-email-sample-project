using MimeKit;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IBlacklistService
    {
        void RemoveBlacklistedRecipients(MimeMessage mimeMessage, string emailId);
    }
}
