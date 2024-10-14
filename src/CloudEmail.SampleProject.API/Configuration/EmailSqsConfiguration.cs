namespace CloudEmail.SampleProject.API.Configuration
{
    public class EmailSqsConfiguration
    {
        public string TargetQueueUrl { get; set; }

        public string ResponseQueueUrl { get; set; }
    }
}
