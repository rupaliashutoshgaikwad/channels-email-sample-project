using CloudEmail.SampleProject.API.Automation.Builders.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System.Collections.Generic;
using System.Linq;

namespace CloudEmail.SampleProject.API.Automation.Builders
{
    public class CustomSmtpConfigurationTestDataManager : ICustomSmtpConfigurationTestDataManager
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public CustomSmtpConfigurationTestDataManager(ConfigurationFixture configurationFixture)
        {
            _host = configurationFixture.Configuration["ChannelsGmailImapAccess:Host"];
            _port = int.Parse(configurationFixture.Configuration["ChannelsGmailImapAccess:Port"]);
            _username = configurationFixture.Configuration["ChannelsGmailImapAccess:Username"];
            _password = configurationFixture.Configuration["ChannelsGmailImapAccess:Password"];
        }

        public List<MimeMessage> GetUnreadMessages()
        {
            using (ImapClient client = new ImapClient { ServerCertificateValidationCallback = (s, c, ch, e) => true })
            {
                client.Connect(_host, _port, SecureSocketOptions.SslOnConnect);
                client.Authenticate(_username, _password);

                client.Inbox.Open(FolderAccess.ReadOnly);

                var unreadMessageUids = client.Inbox.Search(SearchQuery.All);
                var unreadMessages = unreadMessageUids.Select(unreadEmailUid => client.Inbox.GetMessage(unreadEmailUid)).ToList();

                client.Disconnect(true);

                return unreadMessages;
            }
        }

        public List<MimeMessage> GetMessagesWithSubject(string subject)
        {
            using (ImapClient client = new ImapClient { ServerCertificateValidationCallback = (s, c, ch, e) => true })
            {
                client.Connect(_host, _port, SecureSocketOptions.SslOnConnect);
                client.Authenticate(_username, _password);

                client.Inbox.Open(FolderAccess.ReadOnly);

                var messageUids = client.Inbox.Search(SearchQuery.SubjectContains(subject));
                var messages = messageUids.Select(m => client.Inbox.GetMessage(m)).ToList();

                client.Disconnect(true);

                return messages;
            }
        }

        public void DeleteMessage(string messageId)
        {
            using (ImapClient client = new ImapClient { ServerCertificateValidationCallback = (s, c, ch, e) => true })
            {
                client.Connect(_host, _port, SecureSocketOptions.SslOnConnect);
                client.Authenticate(_username, _password);

                client.Inbox.Open(FolderAccess.ReadWrite);

                var uids = client.Inbox.Search(SearchQuery.HeaderContains("Message-Id", messageId));
                client.Inbox.AddFlags(uids, MessageFlags.Deleted, true);

                client.Disconnect(true);
            }
        }
    }
}
