using CloudEmail.Common;
using CloudEmail.Common.Testing.Library.AutoMoqAttributes;
using CloudEmail.SampleProject.API.Automation.Builders;
using CloudEmail.SampleProject.API.Automation.Builders.Interfaces;
using CloudEmail.SampleProject.API.Automation.Models;
using CloudEmail.SampleProject.API.Client;
using CloudEmail.SampleProject.API.Models.Requests;
using CloudEmail.SampleProject.API.Models.Responses;
using FluentAssertions;
using MimeKit;
using Newtonsoft.Json;
using Polly;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace CloudEmail.SampleProject.API.Automation.Tests.Acceptance.CustomSmtpConfigurationControllerTests
{
    [ExcludeFromCodeCoverage]
    public class SendTestEmailTests : AcceptanceTests, IClassFixture<ConfigurationFixture>
    {
        private readonly ICustomSmtpConfigurationTestDataManager _testDataManager;
        private readonly IRestRequestBuilder _requestBuilder;

        private readonly string _apiBaseUrl;
        private readonly string _apiBasicToken;

        private readonly string _host;
        private readonly int _port;
        private readonly TlsOption _option;
        private readonly string _username;
        private readonly string _password;
        private readonly string _sendAddress;

        public SendTestEmailTests(ConfigurationFixture configurationFixture)
        {
            _testDataManager = new CustomSmtpConfigurationTestDataManager(configurationFixture);

            _apiBaseUrl = configurationFixture.Configuration["SendEmailApiConfiguration:BaseUrl"];
            _apiBasicToken = configurationFixture.Configuration["SendEmailApiConfiguration:BasicToken"];

            _requestBuilder = new RestRequestBuilder(_apiBaseUrl, _apiBasicToken);

            _host = configurationFixture.Configuration["CustomSmtpConfiguration:Host"];
            _port = int.Parse(configurationFixture.Configuration["CustomSmtpConfiguration:Port"]);
            _option = new TlsOption
            {
                Id = int.Parse(configurationFixture.Configuration["CustomSmtpConfiguration:TlsOption:Id"]),
                Option = configurationFixture.Configuration["CustomSmtpConfiguration:TlsOption:Name"]
            };
            _username = configurationFixture.Configuration["ChannelsGmailImapAccess:Username"];
            _password = configurationFixture.Configuration["ChannelsGmailImapAccess:Password"];
            _sendAddress = configurationFixture.Configuration["ChannelsGmailImapAccess:Username"];
        }

        [Theory]
        [InlineAutoMoqData("/CustomSmtpConfiguration/SendTestEmail")]
        public async Task GivenValidCustomSmtpConfiguration_WithoutClient_ReturnsSuccessAndVerificationCode(
            string uri
        )
        {
            // ARRANGE
            var request = new TestCustomSmtpConfigurationRequest
            {
                Host = _host,
                Port = _port,
                TlsOption = _option,
                Username = _username,
                Password = _password,
                ToAddress = _sendAddress,
                FromAddress = "valid-smtp-test-without-client@email-api-test.com",
                SmtpServerVerificationToken = Guid.NewGuid().ToString()
            };

            var expectedSubject = $"Verification Token: {request.SmtpServerVerificationToken}";

            var restRequest = await _requestBuilder.CreatePostRestRequestWithHeader(uri);
            restRequest.AddParameter("text/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);

            var restClient = new RestClient(_apiBaseUrl);

            // ACT
            var restResponse = restClient.Execute(restRequest);

            // ASSERT
            restResponse.Should().NotBeNull();
            restResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var response = JsonConvert.DeserializeObject<TestCustomSmtpConfigurationResponse>(restResponse.Content);
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeTrue();
            response.ErrorMessage.Should().BeNullOrEmpty();

            var retryPolicy = Policy
                .HandleResult<List<MimeMessage>>(r => r == null || !r.Any(m => m.Subject.Contains(request.SmtpServerVerificationToken)))
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var messagesWithMatchingSubject = retryPolicy.Execute(() => _testDataManager.GetMessagesWithSubject(expectedSubject));

            var testMessage = messagesWithMatchingSubject.Single(m => m.Subject.Contains(request.SmtpServerVerificationToken));
            testMessage.Should().NotBeNull();
            testMessage.Subject.Should().Be(expectedSubject);

            // CLEANUP
            _testDataManager.DeleteMessage(testMessage.MessageId);
        }

        [Fact]
        public async Task GivenValidCustomSmtpConfiguration_WithClient_ReturnsSuccessAndVerificationCode()
        {
            // ARRANGE
            var request = new TestCustomSmtpConfigurationRequest
            {
                Host = _host,
                Port = _port,
                TlsOption = _option,
                Username = _username,
                Password = _password,
                ToAddress = _sendAddress,
                FromAddress = "valid-smtp-test-with-client@email-api-test.com",
                SmtpServerVerificationToken = Guid.NewGuid().ToString()
            };

            var expectedSubject = $"Verification Token: {request.SmtpServerVerificationToken}";

            var customSmtpConfigurationClient = new CustomSmtpConfigurationClient(_apiBaseUrl, _apiBasicToken);

            // ACT
            var response = await customSmtpConfigurationClient.SendTestEmail(request);

            // ASSERT
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeTrue();
            response.ErrorMessage.Should().BeNullOrEmpty();

            var retryPolicy = Policy
                .HandleResult<List<MimeMessage>>(r => r == null || !r.Any(m => m.Subject.Contains(request.SmtpServerVerificationToken)))
                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var messagesWithMatchingSubject = retryPolicy.Execute(() => _testDataManager.GetMessagesWithSubject(expectedSubject));

            var testMessage = messagesWithMatchingSubject.Single(m => m.Subject.Contains(request.SmtpServerVerificationToken));
            testMessage.Should().NotBeNull();
            testMessage.Subject.Should().Be(expectedSubject);

            // CLEANUP
            _testDataManager.DeleteMessage(testMessage.MessageId);
        }

        [Theory]
        [InlineAutoMoqData("/CustomSmtpConfiguration/SendTestEmail")]
        public async Task GivenInvalidCustomSmtpConfiguration_WithoutClient_ReturnsFailureAndErrorMessage(
            string uri,
            string invalidHost
        )
        {
            // ARRANGE
            var request = new TestCustomSmtpConfigurationRequest
            {
                Host = invalidHost,
                Port = _port,
                TlsOption = _option,
                Username = _username,
                Password = _password,
                ToAddress = _sendAddress,
                FromAddress = "invalid-smtp-test-without-client@email-api-test.com"
            };

            var restRequest = await _requestBuilder.CreatePostRestRequestWithHeader(uri);
            restRequest.AddParameter("text/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);

            var restClient = new RestClient(_apiBaseUrl);

            // ACT
            var restResponse = restClient.Execute(restRequest);

            // ASSERT
            restResponse.Should().NotBeNull();
            restResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var response = JsonConvert.DeserializeObject<TestCustomSmtpConfigurationResponse>(restResponse.Content);
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeFalse();
            response.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [AutoMoqData]
        public async Task GivenInvalidCustomSmtpConfiguration_WithClient_ReturnsFailureAndErrorMessage(
            string invalidHost
        )
        {
            // ARRANGE
            var request = new TestCustomSmtpConfigurationRequest
            {
                Host = invalidHost,
                Port = _port,
                TlsOption = _option,
                Username = _username,
                Password = _password,
                ToAddress = _sendAddress,
                FromAddress = "invalid-smtp-test-with-client@email-api-test.com"
            };

            var customSmtpConfigurationClient = new CustomSmtpConfigurationClient(_apiBaseUrl, _apiBasicToken);

            // ACT
            var response = await customSmtpConfigurationClient.SendTestEmail(request);

            // ASSERT
            response.Should().NotBeNull();
            response.TestEmailSentSuccessfully.Should().BeFalse();
            response.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineAutoMoqData("/CustomSmtpConfiguration/SendTestEmail")]
        public void GivenInvalidToken_WithoutClient_ReturnsUnauthorizedResponse(
            string uri,
            string domain
        )
        {
            // ARRANGE
            var request = new RestRequest(string.Format(uri, domain), Method.POST);
            var restClient = new RestClient(_apiBaseUrl);

            // ACT
            var response = restClient.Execute(request);

            // ASSERT
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
