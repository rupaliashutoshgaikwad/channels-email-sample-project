using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface IStorageService
    {
        Task<T> GetObjectFromStorage<T>(string storageKey);
        Task PutObjectToStorage<T>(T obj, string storageKey);
    }
}
