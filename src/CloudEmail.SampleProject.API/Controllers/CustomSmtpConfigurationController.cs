using System;
using System.Threading.Tasks;
using CloudEmail.API.Models.Enums;
using CloudEmail.Common;
using CloudEmail.SampleProject.API.Models.Responses;
using CloudEmail.SampleProject.API.Models.Requests;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;

namespace CloudEmail.SampleProject.API.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class CustomSmtpConfigurationController : ControllerBase
    {
        private readonly ISmtpService smtpService;
        private readonly IEmailAuditService emailAuditService;

        public CustomSmtpConfigurationController(ISmtpService smtpService,
            IEmailAuditService emailAuditService)
        {
            this.smtpService = smtpService;
            this.emailAuditService = emailAuditService;
        }

        [HttpPost("SendTestEmail")]
        public async Task<ActionResult<TestCustomSmtpConfigurationResponse>> SendTestEmail(TestCustomSmtpConfigurationRequest request)
        {
            var emailId = Guid.NewGuid().ToString();
            emailAuditService.LogCustomSmtpTestEmailStart(emailId);

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = "This is your SMTP Server verification email";

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(request.FromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(request.ToAddress));
            mimeMessage.Subject = $"Verification Token: {request.SmtpServerVerificationToken}";
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            if (request.AuthenticationOption == null)
			{
                request.AuthenticationOption = new AuthenticationOption { Id = 1 };
			}

            var sendEmailResponse = await smtpService.SendCustomSmtp(mimeMessage, request.Host, request.Port, request.TlsOption, request.Username, request.Password, emailId, request.AuthenticationOption.Id, request.CertificateData);

            var response = new TestCustomSmtpConfigurationResponse { TestEmailSentSuccessfully = sendEmailResponse.SendEmailResponseCode == SendEmailResponseCode.Success };

            if (!response.TestEmailSentSuccessfully)
            {
                response.ErrorMessage = sendEmailResponse.ErrorMessage;
            }

            return response;
        }
    }
}
