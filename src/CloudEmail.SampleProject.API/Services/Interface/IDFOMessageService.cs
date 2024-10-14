using Channels.DFO.Api.Client.Services.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using Channels.DFO.Api.Model.Integration;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IDFOMessageService
    {
        Task<bool> UpdateMessageAttribute(string channelId, string messageIdOnExternalPlatform, string sendMessageId, string tenantId);
    }
}
