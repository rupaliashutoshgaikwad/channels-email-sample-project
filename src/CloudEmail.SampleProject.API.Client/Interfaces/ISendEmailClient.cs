using CloudEmail.API.Models.Responses;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Client.Interfaces
{
    public interface ISendEmailClient
    {
        Task<SendEmailResponse> SendEmailFromStorage(string emailId, int queueReceiveCount, string sentTimeStampString);
    }
}
