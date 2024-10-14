using System.Threading.Tasks;
using CloudEmail.Common;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ICustomSmtpConfigurationService
    {
        Task<CloudCustomSmtpSettings> GetCustomSmtpConfiguration(int businessUnit, string domain);
    }
}
