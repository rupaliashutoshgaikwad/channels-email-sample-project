using CloudEmail.SampleProject.API.Models.Requests;
using CloudEmail.SampleProject.API.Models.Responses;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Client.Interfaces
{
    public interface ICustomSmtpConfigurationClient
    {
        Task<TestCustomSmtpConfigurationResponse> SendTestEmail(TestCustomSmtpConfigurationRequest request);
    }
}
