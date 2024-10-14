using CloudEmail.API.Models.Requests;
using CloudEmail.SampleProject.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class TestingController : ControllerBase
    {
        private readonly IStorageService storageService;

        public TestingController(IStorageService storageService)
        {
            this.storageService = storageService;
        }

        [HttpPost("PutEmailToStorage")]
        public async Task<ActionResult> PutEmailToStorage(SendEmailRequest request, [FromQuery] string emailId)
        {
            await storageService.PutObjectToStorage(request, emailId);
            return Ok();
        }
    }
}
