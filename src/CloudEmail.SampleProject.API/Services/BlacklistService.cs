using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Linq;

namespace CloudEmail.SampleProject.API.Services
{
    public class BlacklistService : IBlacklistService
    {
        private readonly IBlackListClient _blackListClient;
        private readonly ILogger<BlacklistService> _logger;

        public BlacklistService(
            IBlackListClient blackListClient,
            ILogger<BlacklistService> logger)
        {
            _blackListClient = blackListClient;
            _logger = logger;
        }

        public void RemoveBlacklistedRecipients(MimeMessage mimeMessage, string emailId)
        {
            var originalRecipientCount = mimeMessage.To.Count + mimeMessage.Cc.Count + mimeMessage.Bcc.Count;

            RemoveBlacklistedRecipientsForList(mimeMessage.To);
            RemoveBlacklistedRecipientsForList(mimeMessage.Cc);
            RemoveBlacklistedRecipientsForList(mimeMessage.Bcc);

            var postBlacklistRecipientCount = mimeMessage.To.Count + mimeMessage.Cc.Count + mimeMessage.Bcc.Count;

            if (postBlacklistRecipientCount == 0)
            {
                _logger.LogInformation($"RemoveBlacklistedRecipients > EmailId: {emailId} All email recipients were found on the blacklist.");
            }

            if (originalRecipientCount != postBlacklistRecipientCount)
            {
                _logger.LogInformation($"RemoveBlacklistedRecipients > EmailId: {emailId} Some email recipients were found on the blacklist.");
            }
        }

        private async void RemoveBlacklistedRecipientsForList(InternetAddressList mimeAddressList)
        {
            if (!mimeAddressList.Mailboxes.Any())
            {
                return;
            }

            var recipientList = mimeAddressList.Mailboxes.Select(x => x.Address).ToList();

            var blacklistedRecipients = (await _blackListClient.GetBlacklistItems(recipientList)).Select(x => x.Address).ToList();

            //var blacklistedRecipients = _blacklistRepo.GetBlacklistItems(recipientList).Select(x => x.Address).ToList();
            recipientList.RemoveAll(x => blacklistedRecipients.Any(rec => rec == x));

            mimeAddressList.Clear();
            mimeAddressList.AddRange(recipientList.Select(r => MailboxAddress.Parse(r)));
        }
    }
}
