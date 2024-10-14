using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using CloudEmail.SampleProject.API.Services;

namespace CloudEmail.SampleProject.API.Controllers
{
	[ExcludeFromCodeCoverage]
	public class HealthCheckController : Controller
	{
		private readonly HealthCheckService _healthCheckService;
		public HealthCheckController(HealthCheckService healthCheckService)
		{
			_healthCheckService = healthCheckService;
		}

		[HttpGet("api/v1/HealthCheck")]
		[HttpGet("HealthCheck")]
		public IActionResult Get()
		{
			return Ok();
		}

		/// <summary>
		///     Get Extended Health Check
		/// </summary>
		/// <remarks>Provides an indication about the health of the API</remarks>
		/// <response code="200">API is healthy</response>
		/// <response code="503">API is unhealthy or in degraded state</response> 
		[HttpGet("api/v1/ExtendedHealthCheck")]
		[HttpGet("ExtendedHealthCheck")]
		[ProducesResponseType(typeof(HealthReport), (int)HttpStatusCode.OK)]
		public async Task<IActionResult> GetExtended()
		{
			var report = await _healthCheckService.CheckHealthAsync();

			return report.Status == HealthStatus.Healthy ? Ok(report) : StatusCode((int)HttpStatusCode.ServiceUnavailable, report);
		}

		[HttpGet]
		[Route("myRoute")]
        public async Task<ActionResult<string>> ReceiveMessages()
		{
            var messages = new List<string> { "Message 1", "Message 2" };

            // Returning Ok (200) with the list of messages
            return Ok(messages);
        }

    }
}
