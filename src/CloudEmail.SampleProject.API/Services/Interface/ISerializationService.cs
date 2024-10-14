using System.IO;

namespace CloudEmail.SampleProject.API.Services.Interface
{
    public interface ISerializationService
    {
        T DeserializeStreamIntoObject<T>(Stream stream);

        string SerializeToJsonString(object obj);
    }
}
