using System.Net.Mail;
using System.Threading.Tasks;
using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using CloudEmail.Common;
using MimeKit;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IPublishResultsService
    {
        Task PublishSendEmailResults(SendEmailRequest sendEmailRequest, string emailId, EdgeType edgeType, SendEmailResponse sendEmailResponse, MailAddress from, MimeMessage mimeMessage);
        Task PublishSendEmailBlacklist(string businessUnit, EdgeType edgeType);
    }
}
