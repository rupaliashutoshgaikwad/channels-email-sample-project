using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IDomainVerificationService
    {
        Task<bool> IsDomainVerified(string domain);

        void LoadCache();
    }
}
