using Amazon.S3;
using CloudEmail.API.Clients.Interfaces;
using CloudEmail.API.Models.Requests;
using CloudEmail.Common;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class EmailStorageService : IEmailStorageService
    {
        private readonly ICloudWatchClient cloudWatchClient;
        private readonly IEmailAuditService emailAuditService;
        private readonly IStorageService storageService;
        private readonly AmazonS3Configuration amazonS3Configuration;
        private readonly ILogger<EmailStorageService> logger;

        public EmailStorageService(
            IStorageService storageService,
            IEmailAuditService emailAuditService,
            ICloudWatchClient cloudWatchClient,
            IOptions<AmazonS3Configuration> amazonS3Configuration,
            ILogger<EmailStorageService> logger)
        {
            this.storageService = storageService;
            this.emailAuditService = emailAuditService;
            this.cloudWatchClient = cloudWatchClient;
            this.amazonS3Configuration = amazonS3Configuration.Value;
            this.logger = logger;
        }

        public async Task PutEmailToUnsendables(SendEmailRequest sendEmailRequest)
        {
            try
            {
                string storageKey = BuildUnsendablesKey(sendEmailRequest.BusinessUnit, sendEmailRequest.EmailId);
                await storageService.PutObjectToStorage(sendEmailRequest, storageKey);
                emailAuditService.LogPutEmailToUnsendablesSuccess(sendEmailRequest.EmailId, sendEmailRequest.BusinessUnit, sendEmailRequest.ContactId);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString(), e);
                await cloudWatchClient.PublishAsync(sendEmailRequest.BusinessUnit, StorageOutcome.Failure);
                await cloudWatchClient.PublishAsync(StorageAction.Put, StorageOutcome.Failure);
                throw new AmazonS3Exception($"Failed to put email to unsendables. EmailId: {sendEmailRequest.EmailId} - ContactId: {sendEmailRequest.ContactId}", e);
            }
        }

        private string BuildUnsendablesKey(int businessUnit, string emailId)
        {
            return amazonS3Configuration.OutboundUnsendablesPrefix + $"/{businessUnit}/" + emailId;
        }
    }
}
