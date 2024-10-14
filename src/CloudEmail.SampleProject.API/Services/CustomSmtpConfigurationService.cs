using CloudEmail.Common;
using CloudEmail.SampleProject.API.Services.Interface;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.Management.API.Models;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace CloudEmail.SampleProject.API.Services
{
    public class CustomSmtpConfigurationService : ICustomSmtpConfigurationService
    {
        private readonly ILogger<CustomSmtpConfigurationService> _logger;
        private readonly ISmtpServerClient _smtpServerClient;
        private readonly IMemoryCache _memoryCache;

        public CustomSmtpConfigurationService(
            ISmtpServerClient smtpServerClient,
            IMemoryCache memoryCache,
            ILogger<CustomSmtpConfigurationService> logger)
        {
            _smtpServerClient = smtpServerClient;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<CloudCustomSmtpSettings> GetCustomSmtpConfiguration(int businessUnit, string domain)
        {
            _logger.LogInformation($"Calling get Custom Smtp configuration with bu:{businessUnit} and domain: {domain}");
            var smtpServerDetail = await GetSmtpServerByBusinessUnitAndDomain(businessUnit, domain);
            if (smtpServerDetail != null && smtpServerDetail.SmtpServer != null)
            {
                _logger.LogInformation($"SMTP Server found for BU: {businessUnit} and Domain: {domain}. SMTP Server Id: {smtpServerDetail.SmtpServer.Id}");
                var smtpServer = smtpServerDetail.SmtpServer;
                if (smtpServer.AuthenticationOptionId == 2)
                {
                    var smtpServerCertificate = smtpServerDetail.SmtpServerCertificate;
                    if (smtpServerCertificate != null)
                    {
                        _logger.LogInformation($"Authentication Option for {businessUnit} is 2 Certificate {smtpServerCertificate.CertificateFileName} retrieved");
                        return new CloudCustomSmtpSettings(smtpServer.Enabled, smtpServer.Host, smtpServer.Port, smtpServer.Username, smtpServer.Password, smtpServer.TlsOption, smtpServer.AuthenticationOption, smtpServerCertificate.CertificateData);
                    }
                    else
                    {
                        _logger.LogInformation($"Authentication Option for {businessUnit} is 2 but no Certificate retrieved");
                        return new CloudCustomSmtpSettings(smtpServer.Enabled, smtpServer.Host, smtpServer.Port, smtpServer.Username, smtpServer.Password, smtpServer.TlsOption);
                    }
                }
                else
                {
                    _logger.LogInformation($"Authentication Option for {businessUnit} is 1");
                    return new CloudCustomSmtpSettings(smtpServer.Enabled, smtpServer.Host, smtpServer.Port, smtpServer.Username, smtpServer.Password, smtpServer.TlsOption, new AuthenticationOption() { Id = 1 }, new byte[0]);
                }
            }
            else
            {
                _logger.LogInformation($"Get Custom Smtp configuration for BU:{businessUnit} and Domain:{domain}. Results returned: Null. No Smtp found");
                return null;
            }
        }

        private async Task<SmtpServerDetail> GetSmtpServerByBusinessUnitAndDomain(int businessUnit, string domain)
        {
            // Look for cache key.
            if (!_memoryCache.TryGetValue($"_SmtpServerByBusinessUnitAndDomain_{businessUnit}_{domain}", out SmtpServerDetail smtpServerDetail))
            {
                _logger.LogInformation($"Custom smtp setting cache miss - calling management-api for BU:{businessUnit} and Domain:{domain}.");
                SmtpServerDetail result = await _smtpServerClient.GetSmtpServerByBusinessUnitAndDomain(businessUnit, domain);
                smtpServerDetail = result ?? null;

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(300));

                // Save data in cache.
                _memoryCache.Set($"_SmtpServerByBusinessUnitAndDomain_{businessUnit}_{domain}", smtpServerDetail, cacheEntryOptions);
            }
            _logger.LogInformation($"Custom smtp setting cache hit for BU:{businessUnit} and Domain:{domain}.");
            return smtpServerDetail;
        }
    }
}
