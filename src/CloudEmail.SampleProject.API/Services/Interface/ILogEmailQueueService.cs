using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ILogEmailQueueService
    {
        Task<bool> PutToQueue(string objectKey, string sentTimeStamp);
    }
}
