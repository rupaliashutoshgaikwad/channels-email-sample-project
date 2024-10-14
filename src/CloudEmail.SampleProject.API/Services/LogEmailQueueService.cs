using Amazon.SQS;
using Amazon.SQS.Model;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class LogEmailQueueService : ILogEmailQueueService
    {
        private readonly IAmazonSQS sqsClient;
        private readonly ILogger<LogEmailQueueService> logger;
        private readonly string targetQueueUrl;
        private readonly string bucketName;

        public LogEmailQueueService(IAmazonSQS sqsClient, ILogger<LogEmailQueueService> logger, LogEmailSqsConfiguration logEmailSqsConfiguration)
        {
            this.sqsClient = sqsClient;
            this.logger = logger;
            targetQueueUrl = logEmailSqsConfiguration.TargetQueueUrl;
            bucketName = logEmailSqsConfiguration.S3BucketName;
        }

        public async Task<bool> PutToQueue(string objectKey, string sentTimeStamp)
        {
            var sqsRequest = new SendMessageRequest
            {
                QueueUrl = targetQueueUrl,
                MessageBody = objectKey,
                MessageGroupId = objectKey,
                MessageDeduplicationId = Guid.NewGuid().ToString(),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "BucketName", new MessageAttributeValue { DataType = "String", StringValue = bucketName }},
                    { "SentTimeStampString", new MessageAttributeValue { DataType = "String", StringValue = sentTimeStamp }}
                }
            };

            try
            {
                await sqsClient.SendMessageAsync(sqsRequest);
            }
            catch (Exception e)
            {
                logger.LogError($"Amazon SQS Exception - Failed to send email to queue {targetQueueUrl}. Object Key: {objectKey} | Exception: {e.ToString()}");
                return false;
            }

            return true;
        }
    }
}
