using CloudEmail.ApiAuthentication.Models;
using CloudEmail.SampleProject.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudEmail.SampleProject.API.Controllers
{
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class ApplicationRegistrationController : ControllerBase
    {
        private readonly ReadApiDbContext readContext;
        private readonly WriteApiDbContext writeContext;

        public ApplicationRegistrationController(ReadApiDbContext readContext, WriteApiDbContext writeContext)
        {
            this.readContext = readContext;
            this.writeContext = writeContext;
        }

        [HttpGet]
        [Authorize]
        public IEnumerable<ApplicationRegistration> GetApplicationRegistrations()
        {
            return readContext.ApplicationRegistrations;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetApplicationRegistration([FromRoute] int id)
        {
            var applicationRegistration = await readContext.ApplicationRegistrations.FindAsync(id);

            if (applicationRegistration == null)
            {
                return NotFound();
            }

            return Ok(applicationRegistration);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostApplicationRegistration([FromBody] ApplicationRegistration applicationRegistration)
        {
            var existingApplicationRegistration = readContext.ApplicationRegistrations.FirstOrDefault(x => x.Name.Equals(applicationRegistration.Name));
            if (existingApplicationRegistration != null)
            {
                return Conflict();
            }

            applicationRegistration.Token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{applicationRegistration.Name}:basic"));
            applicationRegistration.Created = DateTime.Now.ToUniversalTime();

            writeContext.ApplicationRegistrations.Add(applicationRegistration);
            await writeContext.SaveChangesAsync();

            return CreatedAtAction("GetApplicationRegistration", new { id = applicationRegistration.Id }, applicationRegistration);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteApplicationRegistration([FromRoute] int id)
        {
            var applicationRegistration = await writeContext.ApplicationRegistrations.FindAsync(id);
            if (applicationRegistration == null)
            {
                return NotFound();
            }

            writeContext.ApplicationRegistrations.Remove(applicationRegistration);
            await writeContext.SaveChangesAsync();

            return Ok(applicationRegistration);
        }
    }
}
