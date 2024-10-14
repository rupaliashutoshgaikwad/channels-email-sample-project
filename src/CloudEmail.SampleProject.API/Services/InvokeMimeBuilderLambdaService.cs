using Amazon.Lambda;
using Amazon.Lambda.Model;
using CloudEmail.MimeBuilder.Lambda.Models.Requests;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    [ExcludeFromCodeCoverage]
    public class InvokeMimeBuilderLambdaService : IInvokeMimeBuilderLambdaService
    {
        private readonly AmazonS3Configuration amazonS3Configuration;

        public InvokeMimeBuilderLambdaService(IOptions<AmazonS3Configuration> amazonS3Configuration)
        {
            this.amazonS3Configuration = amazonS3Configuration.Value;
        }

        public async Task<string> InvokeMimeBuilderLambda(string emailId, string sentTimeStampString, int queueReceiveCount)
        {
            var buildMimeRequest = new BuildMimeRequest
            {
                BucketName = amazonS3Configuration.BucketName,
                ObjectKey = emailId,
                SentTimeStampString = sentTimeStampString,
                QueueReceiveCount = queueReceiveCount
            };

            var jsonString = JsonConvert.SerializeObject(buildMimeRequest);
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);

            using (var stream = new MemoryStream(byteArray))
            {
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = "channels-email-mime-builder-lambda",
                    InvocationType = InvocationType.RequestResponse,
                    LogType = LogType.Tail,
                    PayloadStream = stream
                };

                var buildMimeResponse = "";
                using (var client = new AmazonLambdaClient(new AmazonLambdaConfig { Timeout = TimeSpan.FromMinutes(6) }))
                {
                    var invokeResponse = await client.InvokeAsync(invokeRequest);

                    if (invokeResponse != null)
                    {
                        using (var sr = new StreamReader(invokeResponse.Payload))
                        {
                            buildMimeResponse = await sr.ReadToEndAsync();
                        }
                    }

                    return buildMimeResponse;
                }
            }
        }
    }
}
