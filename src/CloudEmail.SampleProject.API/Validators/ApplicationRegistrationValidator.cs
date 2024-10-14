using CloudEmail.ApiAuthentication.Models;
using FluentValidation;

namespace CloudEmail.SampleProject.API.Validators
{
    public class ApplicationRegistrationValidator : AbstractValidator<ApplicationRegistration>
    {
        public ApplicationRegistrationValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
