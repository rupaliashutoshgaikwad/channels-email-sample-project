namespace CloudEmail.SampleProject.API.Configuration
{
    public class AmazonS3Configuration
    {
        public string BucketName { get; set; }
        public string OutboundUnsendablesPrefix { get; set; }
    }
}
