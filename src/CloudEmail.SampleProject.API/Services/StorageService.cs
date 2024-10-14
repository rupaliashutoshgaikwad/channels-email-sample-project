using Amazon.S3;
using Amazon.S3.Model;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class StorageService : IStorageService
    {
        private readonly AmazonS3Configuration amazonS3Configuration;
        private readonly IAmazonS3 s3Client;
        private readonly ISerializationService serializationService;

        public StorageService(IOptions<AmazonS3Configuration> amazonS3Configuration, IAmazonS3 s3Client, ISerializationService serializationService)
        {
            this.amazonS3Configuration = amazonS3Configuration.Value;
            this.s3Client = s3Client;
            this.serializationService = serializationService;
        }

        public async Task<T> GetObjectFromStorage<T>(string storageKey)
        {
            var request = new GetObjectRequest
            {
                BucketName = amazonS3Configuration.BucketName,
                Key = storageKey
            };

            using (GetObjectResponse response = await s3Client.GetObjectAsync(request))
            {
                using (Stream responseStream = response.ResponseStream)
                {
                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        throw new AmazonS3Exception($"Failed to get object from S3. Key: {storageKey}");
                    }

                    return serializationService.DeserializeStreamIntoObject<T>(responseStream);
                }
            }
        }

        public async Task PutObjectToStorage<T>(T obj, string storageKey)
        {
            var jsonBody = serializationService.SerializeToJsonString(obj);

            var putRequest = new PutObjectRequest
            {
                BucketName = amazonS3Configuration.BucketName,
                Key = storageKey,
                ContentBody = jsonBody
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new AmazonS3Exception($"Failed to put object onto S3. Key: {storageKey}");
            }
        }
    }
}
