using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ICloudStorageQueueService
    {
        Task<bool> PutToQueue(string objectKey, string sentTimeStamp);
    }
}
