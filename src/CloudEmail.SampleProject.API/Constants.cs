using System.Diagnostics.CodeAnalysis;

namespace CloudEmail.SampleProject.API
{
    [ExcludeFromCodeCoverage]
    public static class Constants
    {
        public static string ApiIssuer => "Authorization:Issuers:API";

        public static string ApiIssuerSecret => "Authorization:ApiSecret";

        public static string ReadApiDatabase => "ReadApiDatabase";

        public static string WriteApiDatabase => "WriteApiDatabase";

        public static string ReadEmailDatabase => "ReadEmailDatabase";

        public static string MyGlobalDatabase => "MyGlobalDatabase";

        public static string AmazonS3Configuration => "AmazonS3Configuration";

        public static string KerioHost => "SmtpServiceConfiguration:KerioHost";

        public static string VerifiedDomainsCacheDuration => "VerifiedDomains:CacheDuration";

        public static string UnverifiedDomainsCacheDuration => "UnverifiedDomains:CacheDuration";

        public static string RetryCountConfiguration => "RetryCountConfiguration";

        public static string LogEmailSqsConfiguration => "LogEmailSqsConfiguration";

        public static string CloudStorageSqsConfiguration => "CloudStorageSqsConfiguration";

        public static string AmazonSESConfiguration => "AmazonSESConfiguration";

        public const string EmailManagementApiBaseUrl = "EmailManagementApi:BaseUrl";

        public const string EmailManagementApiKey = "EmailManagementApi:ApiKey";

        public const string EmailMetadataApiSection = "EmailMetadataApi";

        public const string EmailFeatureToggleBaseUrl = "EmailManagementApi:EmailFeatureToggleEndpointBaseUrl";

        public const string EmailSqsConfiguration = "EmailSqsConfiguration";
    }
}