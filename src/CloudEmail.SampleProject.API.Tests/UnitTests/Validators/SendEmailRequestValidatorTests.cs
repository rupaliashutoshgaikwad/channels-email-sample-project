using CloudEmail.API.Models;
using CloudEmail.API.Models.Requests;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Validators;
using FluentValidation.TestHelper;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Validators
{
    [ExcludeFromCodeCoverage]
    public class SendEmailRequestValidatorTests
    {
        [Theory]
        [AutoMoqData]
        public void SendEmailRequestValidator_GivenValidRequest_NoValidationError(
            int busNo,
            string contactId,
            MimeWrapper mimeWrapper,
            SendEmailRequestValidator validator
        )
        {
            // ARRANGE
            var request = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper
            };

            // ACT
            var result = validator.Validate(request);

            // ASSERT
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal(0, result.Errors.Count);
        }

        [Theory]
        [AutoMoqData]
        public void SendEmailRequestValidator_GivenInvalidBusinessUnit_ValidationError(
            string contactId,
            MimeWrapper mimeWrapper,
            SendEmailRequestValidator validator
        )
        {
            // ARRANGE
            var request = new SendEmailRequest
            {
                ContactId = contactId,
                MimeWrapper = mimeWrapper
            };

            // ACT / ASSERT
            validator.ShouldHaveValidationErrorFor(x => x.BusinessUnit, request);
        }

        [Theory]
        [InlineAutoMoqData(1, "")]
        public void SendEmailRequestValidator_GivenInvalidContactId_ValidationError(
            int busNo,
            string contactId,
            MimeWrapper mimeWrapper,
            SendEmailRequestValidator validator
        )
        {
            // ARRANGE
            var request = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = mimeWrapper
            };

            // ACT / ASSERT
            validator.ShouldHaveValidationErrorFor(x => x.ContactId, request);
        }

        [Theory]
        [AutoMoqData]
        public void SendEmailRequestValidator_GivenNullMime_ValidationError(
            int busNo,
            string contactId,
            SendEmailRequestValidator validator
        )
        {
            // ARRANGE
            var request = new SendEmailRequest
            {
                BusinessUnit = busNo,
                ContactId = contactId,
                MimeWrapper = null
            };

            // ACT / ASSERT
            validator.ShouldHaveValidationErrorFor(x => x.MimeWrapper, request);
        }
    }
}
