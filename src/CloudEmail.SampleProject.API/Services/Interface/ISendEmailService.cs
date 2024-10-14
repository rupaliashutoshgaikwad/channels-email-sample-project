using CloudEmail.API.Models.Requests;
using CloudEmail.API.Models.Responses;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ISendEmailService
    {
        Task<SendEmailResponse> SendRoutedEmail(SendEmailRequest sendEmailRequest, int queueReceiveCount);
        Task<bool> UpdateDFOExternalAttribute(SendEmailRequest sendEmailRequest, SendEmailResponse sendEmailResponse);
    }
}
