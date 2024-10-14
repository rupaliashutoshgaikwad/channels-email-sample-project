using CloudEmail.SampleProject.API.Services.Interface;
using Newtonsoft.Json;
using System.IO;

namespace CloudEmail.SampleProject.API.Services
{
    public class SerializationService : ISerializationService
    {
        public string SerializeToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T DeserializeStreamIntoObject<T>(Stream stream)
        {
            var reader = new StreamReader(stream).ReadToEnd();
            return JsonConvert.DeserializeObject<T>(reader);
        }
    }
}
