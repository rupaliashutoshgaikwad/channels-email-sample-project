using AutoFixture.Xunit2;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.Common.Models;
using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.SampleProject.API.Services;
using Moq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class BlacklistServiceTests
    {
        private readonly MimeMessageTestDataManager _testDataManager = new MimeMessageTestDataManager();

        [Theory]
        [AutoMoqData]
        public void GivenEmptyBlacklist_ReturnsMimeMessageWithAllRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 2);

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(new List<BlacklistItem>());

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 2, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenNonMatchingBlacklist_ReturnsMimeMessageWithAllRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = "not-a-recipient@test.com" } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(new List<BlacklistItem>());

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 2, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenOnlyToRecipients_ReturnsMimeMessageWithToRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 0, 0);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = "not-a-recipient@test.com" } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 0, 0);
        }

        [Theory]
        [AutoMoqData]
        public void GivenOnlyCcRecipients_ReturnsMimeMessageWithToRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(0, 2, 0);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = "not-a-recipient@test.com" } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 0, 2, 0);
        }

        [Theory]
        [AutoMoqData]
        public void GivenOnlyBccRecipients_ReturnsMimeMessageWithToRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(0, 0, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = "not-a-recipient@test.com" } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 0, 0, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenOnlyToAndCcRecipients_ReturnsMimeMessageWithToAndCcRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 1, 0);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = "not-a-recipient@test.com" } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 1, 0);
        }

        [Theory]
        [AutoMoqData]
        public void GivenPartiallyBlacklistedToList_PublishesEventAndReturnsMimeMessageWithFewerToRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.To.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 1, 2, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenPartiallyBlacklistedCcList_PublishesEventAndReturnsMimeMessageWithFewerCcRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.Cc.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 1, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenPartiallyBlacklistedBccList_PublishesEventAndReturnsMimeMessageWithFewerBccRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.Bcc.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 2, 1);
        }

        [Theory]
        [AutoMoqData]
        public void GivenFullyBlacklistedToList_PublishesEventAndReturnsMimeMessageWithoutToRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(1, 2, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.To.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 0, 2, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenFullyBlacklistedCcList_PublishesEventAndReturnsMimeMessageWithoutCcRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 1, 2);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.Cc.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 0, 2);
        }

        [Theory]
        [AutoMoqData]
        public void GivenFullyBlacklistedBccList_PublishesEventAndReturnsMimeMessageWithoutBccRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(2, 2, 1);
            var blacklist = new List<BlacklistItem> { new BlacklistItem { Address = mimeMessage.Bcc.Mailboxes.First().Address } };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 2, 2, 0);
        }

        [Theory]
        [AutoMoqData]
        public void GivenAllRecipientsAreBlacklisted_PublishesEventAndReturnsMimeMessageWithoutRecipients(
            [Frozen] Mock<IBlackListClient> blackListClient,
            BlacklistService blacklistService,
            string emailId
        )
        {
            // ARRANGE
            var mimeMessage = _testDataManager.BuildTestMimeMessage(1, 1, 1);
            var blacklist = new List<BlacklistItem>
            {
                new BlacklistItem { Address = mimeMessage.To.Mailboxes.First().Address },
                new BlacklistItem { Address = mimeMessage.Cc.Mailboxes.First().Address },
                new BlacklistItem { Address = mimeMessage.Bcc.Mailboxes.First().Address }
            };

            blackListClient.Setup(x => x.GetBlacklistItems(It.IsAny<List<string>>())).ReturnsAsync(blacklist);

            // ACT
            blacklistService.RemoveBlacklistedRecipients(mimeMessage, emailId);

            // ASSERT
            _testDataManager.VerifyMimeMessageRecipientCounts(mimeMessage, 0, 0, 0);
        }
    }
}
