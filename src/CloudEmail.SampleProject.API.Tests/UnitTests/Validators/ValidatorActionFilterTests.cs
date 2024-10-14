using AutoFixture.Xunit2;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CloudEmail.SampleProject.API.Tests.UnitTests.Validators
{
    [ExcludeFromCodeCoverage]
    public class ValidatorActionFilterTests
    {
        [Theory]
        [AutoMoqDataOmitProperties]
        public void OnActionExecuting_GivenInValidModelState_ReturnBadRequestObjectResult(
            [Frozen] ValidatorActionFilter validatorActionFilter,
            ActionExecutingContext actionExecutingContext,
            string key, 
            Exception exception, 
            ModelMetadata metadata
        )
        {
            // ARRANGE
            actionExecutingContext.ModelState.AddModelError(key, exception, metadata);
            var expected = new BadRequestObjectResult(actionExecutingContext.ModelState);

            // ACT
            validatorActionFilter.OnActionExecuting(actionExecutingContext);

            // ASSERT
            Assert.NotNull(actionExecutingContext);
            Assert.Equal(expected.GetType(), actionExecutingContext.Result.GetType());
        }

        [Theory]
        [AutoMoqDataOmitProperties]
        public void OnActionExecuted_GivenInValidModelState_Returns(
            [Frozen] ValidatorActionFilter validatorActionFilter,
            ActionExecutedContext actionExecutedContext
        )
        {
            // ASSERT / ACT
            validatorActionFilter.OnActionExecuted(actionExecutedContext);

            // ASSERT
            Assert.NotNull(actionExecutedContext);
        }
    }
}
