using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IFeatureToggleService
    {
        public Task<bool> GetFeatureToggle(string toggleName);
    }
}
