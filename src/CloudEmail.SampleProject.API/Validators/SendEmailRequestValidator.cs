using CloudEmail.API.Models.Requests;
using FluentValidation;

namespace CloudEmail.SampleProject.API.Validators
{
    public class SendEmailRequestValidator : AbstractValidator<SendEmailRequest>
    {
        public SendEmailRequestValidator()
        {
            RuleFor(x => x.BusinessUnit)
                .NotEmpty()
                .GreaterThan(0);
            RuleFor(x => x.ContactId)
                .NotEmpty();
            RuleFor(x => x.MimeWrapper)
                .NotEmpty();
        }
    }
}
