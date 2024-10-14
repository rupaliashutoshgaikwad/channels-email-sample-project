using System.Threading.Tasks;
using CloudEmail.API.Models.Enums;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using MimeKit;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ISmtpService
    {
        Task<SendEmailResponse> SendSes(MimeMessage mimeMessage, string emailId, bool enforceTLS);
        Task<SendEmailResponse> SendKerio(MimeMessage mimeMessage, string emailId);
        Task<SendEmailResponse> SendCustomSmtp(MimeMessage mimeMessage, string host, int port, TlsOption tlsOption, string username, string password, string emailId, int authenticationOptionId = 1, byte[] certificateData = null, int businessUnit = 0, EmailType emailType = EmailType.CallCenter);
    }
}
