namespace CloudEmail.SampleProject.API.Configuration
{
    public class LogEmailSqsConfiguration
    {
        public string TargetQueueUrl { get; set; }
        public string S3BucketName { get; set; }
    }
}
