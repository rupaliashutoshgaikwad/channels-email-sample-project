using System.Threading.Tasks;
using CloudEmail.Common;

namespace CloudEmail.SampleProject.API.Clients.Interfaces
{
    public interface IPublishResultsClient
    {
        Task PublishSendEmailSuccess(string emailId, string messageId, string businessUnit, EdgeType edgeType, string from, string toAddresses);
        Task PublishSendEmailFailure(string businessUnit, EdgeType edgeType);
        Task PublishSendEmailDropped(string domain, string businessUnit);
    }
}
