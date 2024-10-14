using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MailKit.Net.Smtp;
using MimeKit;

namespace CloudEmail.SampleProject.API.Wrappers
{
    [ExcludeFromCodeCoverage]
    public class SmtpClientWrapper : SmtpClient
    {
        private readonly List<SmtpCommandException> _exceptions = new List<SmtpCommandException>();

        protected override void OnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            _exceptions.Clear();
        }

        protected override void OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            try
            {
                base.OnRecipientNotAccepted(message, mailbox, response);
            }
            catch (SmtpCommandException ex)
            {
                _exceptions.Add(ex);
            }
        }

        protected override void OnNoRecipientsAccepted(MimeMessage message)
        {
            if (_exceptions.Count == 1)
                throw _exceptions[0];

            throw new AggregateException(_exceptions.ToArray());
        }
    }
}
