using FluentAssertions;
using MimeKit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests
{
    public class MimeMessageTestDataManager
    {
        public MimeMessage BuildTestMimeMessage(int toRecipientCount, int ccRecipientCount, int bccRecipientCount)
        {
            var mimeMessage = new MimeMessage();

            for (var i = 0; i < toRecipientCount; i++)
            {
                mimeMessage.To.Add(MailboxAddress.Parse($"to-recipient-{i}@to-recipients.com"));
            }

            for (var i = 0; i < ccRecipientCount; i++)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse($"cc-recipient-{i}@cc-recipients.com"));
            }

            for (var i = 0; i < bccRecipientCount; i++)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse($"bcc-recipient-{i}@bcc-recipients.com"));
            }

            return mimeMessage;
        }

        public void VerifyMimeMessageRecipientCounts(MimeMessage mimeMessage, int expectedToRecipientCount, int expectedCcRecipientCount, int expectedBccRecipientCount)
        {
            mimeMessage.To.Mailboxes.Should().NotBeNull();
            mimeMessage.To.Mailboxes.Should().HaveCount(expectedToRecipientCount);
            mimeMessage.Cc.Mailboxes.Should().HaveCount(expectedCcRecipientCount);
            mimeMessage.Bcc.Mailboxes.Should().HaveCount(expectedBccRecipientCount);
        }
    }
}
