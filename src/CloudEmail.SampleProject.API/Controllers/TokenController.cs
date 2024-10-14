using CloudEmail.API.Models;
using CloudEmail.ApiAuthentication.Models;
using CloudEmail.SampleProject.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Controllers
{
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ReadApiDbContext readContext;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<TokenController> logger;

        public TokenController(
            IConfiguration configuration,
            ReadApiDbContext readContext,
            ILogger<TokenController> logger)
        {
            this.configuration = configuration;
            this.readContext = readContext;
            this.logger = logger;
        }

        [ProducesResponseType(typeof(ApiToken), 200)]
        [ProducesResponseType(400)]
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<ApiToken>> Get([FromBody] ApiTokenRequest request)
        {
            try
            {
                if (await readContext.ApplicationRegistrations.AnyAsync(ar => ar.Token == request.BasicToken))
                {
                    var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration[AuthConstants.ApiIssuerSecret]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
                    var expirationTime = DateTime.UtcNow.AddDays(60);

                    var token = new JwtSecurityToken(
                        configuration[AuthConstants.ApiIssuer],
                        expires: expirationTime,
                        signingCredentials: creds
                        );

                    var apiToken = new ApiToken { Token = new JwtSecurityTokenHandler().WriteToken(token) };
                    apiToken.ExpirationTime = expirationTime;

                    return apiToken;
                }

                return BadRequest("Application not registered!");
            }
            catch (Exception ex)
            {
                this.logger.LogError("Unable to return token due to exception: {0}", ex.ToString());
                throw;
            }
        }
    }
}