using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        // GET api/email/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Email controller is working");
        }

        // POST api/email/send
        [HttpPost("send")]
        public IActionResult SendEmail([FromBody] EmailRequest request)
        {
            if (request == null)
                return BadRequest("Request body is missing.");

            // Here you will later add your real email logic
            return Ok($"Email sent to {request.To}");
        }

        [HttpGet("health")]
        public IActionResult GetHealth() => Ok("Healthy");
    }
}
