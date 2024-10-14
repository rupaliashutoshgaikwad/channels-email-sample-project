using Amazon.SQS.Model;
using Amazon.SQS;
using CloudEmail.SampleProject.API.Modal;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudEmail.SampleProject.API.Configuration;
using System;

namespace CloudEmail.SampleProject.API.Services
{
    public class SqsService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly string _responseQueueUrl;

        public SqsService(IAmazonSQS sqsClient, IOptions<EmailSqsConfiguration> EmailSqsConfiguration)
        {
            _sqsClient = sqsClient;
            _queueUrl = EmailSqsConfiguration.Value.TargetQueueUrl;
            _responseQueueUrl = EmailSqsConfiguration.Value.ResponseQueueUrl;
        }

        public async Task<List<Message>> ReceiveMessagesAsync()
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10, // Adjust as needed
                WaitTimeSeconds = 10 // Long polling
            };

            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);
            return receiveMessageResponse.Messages;
        }

        public async Task DeleteMessageAsync(string receiptHandle)
        {
            var deleteMessageRequest = new DeleteMessageRequest
            {
                QueueUrl = _queueUrl,
                ReceiptHandle = receiptHandle
            };

            await _sqsClient.DeleteMessageAsync(deleteMessageRequest);
        }
        public async Task SendMessageToResponseQueueAsync(string messageBody)
        {
            var request = new SendMessageRequest
            {
                QueueUrl = _responseQueueUrl,
                MessageBody = messageBody,
                MessageGroupId = Guid.NewGuid().ToString()
            };

            await _sqsClient.SendMessageAsync(request);
        }
    }
}
