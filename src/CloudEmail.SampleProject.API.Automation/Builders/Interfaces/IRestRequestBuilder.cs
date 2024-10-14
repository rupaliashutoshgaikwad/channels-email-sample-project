using RestSharp;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Automation.Builders.Interfaces
{
    public interface IRestRequestBuilder
    {
        Task<RestRequest> CreateGetRestRequestWithHeader(string uri);
        Task<RestRequest> CreatePostRestRequestWithHeader(string uri);
        Task<RestRequest> CreateDeleteRestRequestWithHeader(string uri);
        Task<RestRequest> CreateRestRequestWithHeader(string uri, Method restMethod);
    }
}
