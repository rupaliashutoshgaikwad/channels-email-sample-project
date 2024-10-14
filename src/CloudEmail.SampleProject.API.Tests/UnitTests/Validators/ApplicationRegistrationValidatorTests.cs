using AutoFixture.Xunit2;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Validators;
using FluentValidation.TestHelper;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Validators
{
    [ExcludeFromCodeCoverage]
    public class ApplicationRegistrationValidatorTests
    {
        [Theory]
        [AutoMoqData]
        public void ApplicationRegistrationValidator_GivenValidRequest_NoValidationError(
            [Frozen] ApplicationRegistrationValidator validator,
            ApplicationRegistration applicationRegistration
        )
        {
            // ACT
            var result = validator.Validate(applicationRegistration);

            // ASSERT
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal(0, result.Errors.Count);
        }

        [Theory]
        [AutoMoqData]
        public void ApplicationRegistrationValidator_GivenEmptyName_ValidationError(
            [Frozen] ApplicationRegistrationValidator validator,
            ApplicationRegistration applicationRegistration
        )
        {
            // ARRANGE
            applicationRegistration.Name = string.Empty;

            // ACT / ASSERT
            validator.ShouldHaveValidationErrorFor(x => x.Name, applicationRegistration);
        }
    }
}
