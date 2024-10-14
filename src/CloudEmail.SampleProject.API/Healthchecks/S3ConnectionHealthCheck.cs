using Amazon.S3;
using Amazon.S3.Model;
using HealthChecks.Aws.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.HealthChecks
{
    [ExcludeFromCodeCoverage]
    public class S3ConnectionHealthCheck : IHealthCheck
    {
        public AmazonS3Config S3Configuration { get; }
        public S3BucketOptions BucketOptions { get; }
        public S3ConnectionHealthCheck(AmazonS3Config s3Configuration, S3BucketOptions bucketOptions)
        {
            S3Configuration = s3Configuration;
            BucketOptions = bucketOptions;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                ListObjectsResponse arg = await new AmazonS3Client(S3Configuration).ListObjectsAsync(BucketOptions.BucketName, cancellationToken);
                if (BucketOptions.CustomResponseCheck != null)
                {
                    return BucketOptions.CustomResponseCheck(arg) ? HealthCheckResult.Healthy() : new HealthCheckResult(context.Registration.FailureStatus, "Custom response check is not satisfied.");
                }
                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
            }
        }

    }
}

