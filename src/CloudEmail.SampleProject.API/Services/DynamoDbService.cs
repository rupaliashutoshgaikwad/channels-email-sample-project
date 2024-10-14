using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Amazon;

namespace CloudEmail.SampleProject.API.Services
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private const string TableName = "channels-email-sample-test-table";

        public DynamoDbService(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = new AmazonDynamoDBClient(RegionEndpoint.USWest2);
        }

        public async Task CreateTableAsync()
        {
            var request = new CreateTableRequest
            {
                TableName = TableName,
                AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("Id", ScalarAttributeType.N) // Primary key
            },
                KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("Id", KeyType.HASH) // Partition key
            },
                ProvisionedThroughput = new ProvisionedThroughput(5, 5) // Read and write capacity
            };

            try
            {
                var response = await _dynamoDbClient.CreateTableAsync(request);
                Console.WriteLine($"Table {response.TableDescription.TableName} created successfully.");
            }
            catch (ResourceInUseException)
            {
                Console.WriteLine("Table already exists.");
            }
        }
    }
}
