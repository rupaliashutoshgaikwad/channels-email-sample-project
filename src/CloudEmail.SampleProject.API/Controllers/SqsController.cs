using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using CloudEmail.SampleProject.API.Services;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CloudEmail.SampleProject.API.Controllers
{

    [ApiController]
    public class SqsController : Controller
    {
        private readonly SqsService _sqsService;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly ILogger<SqsController> logger;

        public SqsController(SqsService sqsService, IAmazonDynamoDB dynamoDbClient, ILogger<SqsController> logger)
        {
            _sqsService = sqsService;
            _dynamoDbClient = dynamoDbClient;
            this.logger = logger;
        }


        [HttpGet("receive-messages")]
        public async Task<ActionResult<List<string>>> ReceiveMessages()
        {
            List<string> messageBodies = new List<string>();
            try
            {
                var messages = await _sqsService.ReceiveMessagesAsync();
                if(messages.Count == 0)  return Ok("No message to proccess");
                foreach (var message in messages)
                {
                    try
                    {
                        await SaveMessageToDynamoDB(message);
                        messageBodies.Add(message.Body);
                        // Optionally delete the message after processing
                        await _sqsService.DeleteMessageAsync(message.ReceiptHandle);
                        logger.LogInformation($"Queue message processing complete. MessageId : {message.MessageId}");
                        await _sqsService.SendMessageToResponseQueueAsync($"Processed message: {message.Body}");

                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation($"Error processing message: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Error receiving messages: {ex.Message}");
                return StatusCode(500, "Internal server error while receiving messages.");
            }
            return Ok(messageBodies);
        }

        private async Task SaveMessageToDynamoDB(Message message)
        {
            try
            {
                var document = new Document
                {
                    ["Id"] = DateTime.UtcNow.Ticks.ToString(), // Unique ID based on the current timestamp
                    ["Body"] = message.Body, // Save the message body
                    ["ReceivedAt"] = DateTime.UtcNow.ToString("o") // Store received timestamp
                };

                var tableName = "channels-email-sample-test-table"; // Replace with your actual table name
                var table = Table.LoadTable(_dynamoDbClient, tableName);
                await table.PutItemAsync(document);
                logger.LogInformation($"Queue Message saved to DynamoDB. Message Body: {message.Body}");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Error saving queue message to DynamoDB: {ex.Message}");
                throw; // Re-throw the exception to be handled by the calling method
            }
        }

    }
}
