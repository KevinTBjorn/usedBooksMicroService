using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class GatewayController : ControllerBase {
        [HttpGet]
        public IActionResult Index() {
            return Ok("API Gateway is running");
        }
    }
}