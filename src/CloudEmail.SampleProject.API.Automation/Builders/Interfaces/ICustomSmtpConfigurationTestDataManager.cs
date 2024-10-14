using MimeKit;
using System.Collections.Generic;

namespace CloudEmail.SampleProject.API.Automation.Builders.Interfaces
{
    public interface ICustomSmtpConfigurationTestDataManager
    {
        List<MimeMessage> GetUnreadMessages();
        List<MimeMessage> GetMessagesWithSubject(string subject);
        void DeleteMessage(string messageId);
    }
}
