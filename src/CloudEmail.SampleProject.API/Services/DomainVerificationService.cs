using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class DomainVerificationService : IDomainVerificationService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly ILogger<DomainVerificationService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public DomainVerificationService(
            IAmazonSimpleEmailService sesClient,
            ILogger<DomainVerificationService> logger,
            IMemoryCache memoryCache,
            IConfiguration configuration
            )
        {
            _memoryCache = memoryCache;
            _configuration = configuration;
            _sesClient = sesClient;
            _logger = logger;
        }

        public async void LoadCache()
        {
            _logger.LogInformation("Loading cache..");
            int index = 0;
            var identities = await _sesClient.ListIdentitiesAsync();
            int identities_count = identities.Identities.Count;

            // Set cache options.
            var cacheEntryOptionsVerified = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_configuration.GetValue<int>(Constants.VerifiedDomainsCacheDuration, 1440)));
            var cacheEntryOptionsUnverified = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(_configuration.GetValue<int>(Constants.UnverifiedDomainsCacheDuration, 15)));

            while (identities.Identities.Count > index)
            {
                var identity_request = new GetIdentityVerificationAttributesRequest
                {
                    Identities = identities.Identities.GetRange(index, identities_count >= 100 ? 100 : identities_count)
                };
                var verification_response = await _sesClient.GetIdentityVerificationAttributesAsync(identity_request);

                foreach (var identity in verification_response.VerificationAttributes)
                {
                    // Check if identity is verified
                    if (identity.Value.VerificationStatus == VerificationStatus.Success)
                    {
                        // Save data in cache for 24 hr.
                        _memoryCache.Set(identity.Key, true, cacheEntryOptionsVerified);
                    }
                    else
                    {
                        // Save data for 15 minutes
                        _memoryCache.Set(identity.Key, false, cacheEntryOptionsUnverified);
                    }
                }

                index += 100;
                identities_count -= 100;
            }
        }

        public async Task<bool> IsDomainVerified(string domain)
        {
            // Look for cache key.
            if (!_memoryCache.TryGetValue(domain, out bool domainVerified))
            {
                _logger.LogInformation("Miss");
                // Key not in cache, so get data.
                var domainList = new List<string> { domain };

                try
                {
                    var request = new GetIdentityVerificationAttributesRequest { Identities = domainList };
                    var response = await _sesClient.GetIdentityVerificationAttributesAsync(request);

                    if (response != null)
                    {
                        domainVerified = response.VerificationAttributes.ContainsKey(domain)
                            && response.VerificationAttributes[domain].VerificationStatus == VerificationStatus.Success;

                        // Set cache options.
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromSeconds(_configuration.GetValue<int>(Constants.VerifiedDomainsCacheDuration)));
                        var cacheEntryOptionsUnverified = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(_configuration.GetValue<int>(Constants.UnverifiedDomainsCacheDuration)));

                        // Save data in cache.
                        _memoryCache.Set(domain, domainVerified, domainVerified ? cacheEntryOptions : cacheEntryOptionsUnverified);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to verify domain ({domain}) when calling SES and setting value in cache. " +
                    $"Exception: {ex} DomainVerified: {domainVerified}");
                }
            }
            else
            {
                _logger.LogInformation($"Hit. Verified: {domainVerified}");
            }

            return domainVerified;
        }
    }
}