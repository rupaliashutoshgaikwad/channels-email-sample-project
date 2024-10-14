using CloudEmail.ApiAuthentication.Models;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Automation.Builders.Interfaces
{
    public interface IApplicationRegistrationTestDataManager
    {
        ApplicationRegistration CreateNewApplicationRegistration();
        Task<ApplicationRegistration> CreateAndSaveNewApplicationRegistration();
        Task<ApplicationRegistration> GetApplicationRegistration(string name);
        Task CleanupApplicationRegistration(int id);
        Task<bool> IsValidApplicationRegistrationId(int id);
        Task<int> GetInvalidApplicationRegistrationId();
    }
}
