using CloudEmail.Common.Models;
using CloudEmail.Management.API.Client;
using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.Management.API.Models;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Services
{
    public class FeatureToggleService : IFeatureToggleService
    {
        private readonly ILogger<FeatureToggleService> _logger;
        private readonly IEmailFeatureToggleClient _emailFeatureToggleClient;
        private readonly IMemoryCache _memoryCache;

        public FeatureToggleService(
            IEmailFeatureToggleClient emailFeatureToggleClient,
            IMemoryCache memoryCache,
            ILogger<FeatureToggleService> logger)
        {
            _emailFeatureToggleClient = emailFeatureToggleClient;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<bool> GetFeatureToggle(string toggleName)
        {
            // Look for cache key.
            if (!_memoryCache.TryGetValue($"_FeatureToggle_{toggleName}", out string featureToggleValue))
            {
                // Key not in cache, so get data.
                FeatureToggle result = await _emailFeatureToggleClient.GetEmailFeatureToggle(toggleName);
                featureToggleValue = result?.Value ?? string.Empty;

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(300));

                // Save data in cache.
                _memoryCache.Set($"_FeatureToggle_{toggleName}", featureToggleValue, cacheEntryOptions);
            }
            _logger.LogInformation($"Feature Toggle {toggleName} value: {featureToggleValue}");

            return featureToggleValue == "1";
        }
    }
}
