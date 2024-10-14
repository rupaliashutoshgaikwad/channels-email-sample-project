using AutoFixture.Xunit2;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MailKit.Net.Smtp;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Services
{
    [ExcludeFromCodeCoverage]

    public class ExceptionServiceTests
    {
        [Theory]
        [AutoMoqData]
        public Task ValidateSmtpCommandException_ValidMessage_ReturnTrue(
                ExceptionService target
            )
        {
            // ARRANGE
            var smtpCommandException = new SmtpCommandException(
                SmtpErrorCode.SenderNotAccepted,
                SmtpStatusCode.SystemStatus,
                "5.1.8 Sender address <someaddress@nodomain.com> domain does not exist");

            // ACT
            var result = target.ValidateSmtpCommandException(
                smtpCommandException);

            // ASSERT
            Assert.True(result);
            return Task.CompletedTask;
        }

        [Theory]
        [AutoMoqData]
        public Task ValidateSmtpCommandException_InValidMessage_ReturnFalse(
                ExceptionService target
            )
        {
            // ARRANGE
            var smtpCommandException = new SmtpCommandException(
                SmtpErrorCode.SenderNotAccepted,
                SmtpStatusCode.SystemStatus,
                "Failed while executing smtpcommand");

            // ACT
            var result = target.ValidateSmtpCommandException(
                smtpCommandException);

            // ASSERT
            Assert.False(result);
            return Task.CompletedTask;
        }

        [Theory]
        [AutoMoqData]
        public Task ValidateSmtpCommandException_OtherThanSmtpCommandException_ReturnFalse(
                ExceptionService target
            )
        {
            // ARRANGE
            var exception = new Exception("domain does not exist");

            // ACT
            var result = target.ValidateSmtpCommandException(exception);

            // ASSERT
            Assert.False(result);
            return Task.CompletedTask;
        }
    }
}
