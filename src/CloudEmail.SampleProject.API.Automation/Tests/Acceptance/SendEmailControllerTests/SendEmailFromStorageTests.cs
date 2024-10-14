//using CloudEmail.API.Models;
//using CloudEmail.API.Models.Enums;
//using CloudEmail.API.Models.Requests;
//using CloudEmail.SampleProject.API.Automation.Builders;
//using CloudEmail.SampleProject.API.Automation.Builders.Interfaces;
//using CloudEmail.SampleProject.API.Automation.Models;
//using CloudEmail.SampleProject.API.Client;
//using FluentAssertions;
//using Microsoft.Extensions.Configuration;
//using MimeKit;
//using RestSharp;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Net;
//using System.Threading.Tasks;
//using Xunit;

//namespace CloudEmail.SampleProject.API.Automation.Tests.Acceptance.SendEmailControllerTests
//{
//    [ExcludeFromCodeCoverage]
//    public class SendEmailFromStorageTests : AcceptanceTests, IClassFixture<ConfigurationFixture>
//    {
//        private readonly IRestClient _restClient;
//        private readonly IRestRequestBuilder _requestBuilder;
//        private readonly IConfiguration _configuration;
//        private readonly string _apiBaseUrl;
//        private readonly string _apiBasicToken;
//        private readonly string _fileServerVip;

//        public SendEmailFromStorageTests(ConfigurationFixture configurationFixture)
//        {
//            _configuration = configurationFixture.Configuration;
//            _apiBaseUrl = configurationFixture.Configuration["SendEmailApiConfiguration:BaseUrl"];
//            _apiBasicToken = configurationFixture.Configuration["SendEmailApiConfiguration:BasicToken"];
//            _fileServerVip = configurationFixture.Configuration["SendEmailApiConfiguration:FileServerVip"];
//            _restClient = new RestClient(_apiBaseUrl);
//            _requestBuilder = new RestRequestBuilder(_apiBaseUrl, _apiBasicToken);
//        }

//        [Fact]
//        public async Task GivenEmailId_WithClient_WhenSendEmailFromStorage_ThenReturnOK()
//        {
//            // Arrange
//            var businessUnitNumber = int.Parse(_configuration["KerioConfiguration:BusinessUnitNumber"]);

//            var msg = new MimeMessage();
//            msg.From.Add(new MailboxAddress("send-email-from-storage-acceptance@niceincontact.com"));
//            msg.To.Add(new MailboxAddress(_configuration["KerioConfiguration:ToAddress"]));
//            msg.Subject = $"GivenEmailId_WithClient_WhenSendEmailFromStorage_ThenReturnOK >> {Guid.NewGuid().ToString()}";
//            var wrapperAttachments = new List<WrapperAttachment>();
//            var mimeWrapper = new MimeWrapper(DateTime.Now, msg.To.Select(m => m.ToString()).ToList(), msg.From.Select(m => m.ToString()).ToList(), msg.Cc.Select(m => m.ToString()).ToList(), msg.Bcc.Select(m => m.ToString()).ToList(), msg.Subject, msg.TextBody, msg.HtmlBody, wrapperAttachments);

//            Random rnd = new Random();
//            var emailRequest = new SendEmailRequest
//            {
//                BusinessUnit = businessUnitNumber,
//                ContactId = rnd.Next(1, 99999).ToString(),
//                MimeWrapper = mimeWrapper,
//                EmailId = Guid.NewGuid().ToString(),
//                FileServerVip = _fileServerVip
//            };

//            var request = await _requestBuilder.CreatePostRestRequestWithHeader($"/Testing/PutEmailToStorage?emailId={emailRequest.EmailId}");
//            request.AddJsonBody(emailRequest);

//            var putToStorageResponse = _restClient.Execute(request);
//            putToStorageResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Error: {putToStorageResponse.ErrorMessage}");

//            var sendEmailClient = new SendEmailClient(_apiBaseUrl, _apiBasicToken);

//            // Act
//            var response = await sendEmailClient.SendEmailFromStorage(emailRequest.EmailId, 1, "1590782412227");

//            // Assert
//            response.Should().NotBeNull();
//            response.SendEmailResponseCode.Should().Be(SendEmailResponseCode.Success, $"Error: {response.ErrorMessage}");
//        }
//    }
//}
