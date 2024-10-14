using CloudEmail.ApiAuthentication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Automation.Builders.Interfaces
{
    public interface IApplicationRegistrationBuilder
    {
        Task<bool> CreateApplicationRegistration(ApplicationRegistration registration);
        Task<bool> DeleteApplicationRegistration(int registrationId);
        Task<List<ApplicationRegistration>> GetApplicationRegistrationById(int registrationId);
    }
}
