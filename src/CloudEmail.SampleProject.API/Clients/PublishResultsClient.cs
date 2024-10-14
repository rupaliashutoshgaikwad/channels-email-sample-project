using CloudEmail.API.Clients.Interfaces;
using CloudEmail.Common;
using CloudEmail.SampleProject.API.Clients.Interfaces;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Clients
{
    public class PublishResultsClient : IPublishResultsClient
    {
        private readonly ICloudWatchClient _cloudWatchClient;

        public PublishResultsClient(ICloudWatchClient cloudWatchClient)
        {
            _cloudWatchClient = cloudWatchClient;
        }

        public async Task PublishSendEmailSuccess(string emailId, string messageId, string businessUnit, EdgeType edgeType, string from, string toAddresses)
        {
            await _cloudWatchClient.PublishAsync(edgeType, OutboundOutcome.Success);
            await _cloudWatchClient.PublishAsync(edgeType, OutboundOutcome.Success, businessUnit);
        }

        public async Task PublishSendEmailFailure(string businessUnit, EdgeType edgeType)
        {
            await _cloudWatchClient.PublishAsync(edgeType, OutboundOutcome.Failure);
            await _cloudWatchClient.PublishAsync(edgeType, OutboundOutcome.Failure, businessUnit);
        }

        public async Task PublishSendEmailDropped(string domain, string businessUnit)
        {
            await _cloudWatchClient.PublishAsync(businessUnit, domain);
            await _cloudWatchClient.PublishAsync();
        }
    }
}
