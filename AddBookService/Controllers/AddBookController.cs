using Microsoft.AspNetCore.Mvc;
using Domain;
using AddBookService.Services;
using AddBookService.Models;

namespace AddBookService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddBookController : ControllerBase
    {
        private readonly IAddBookService _addBookService;

        public AddBookController(IAddBookService addBookService)
        {
            _addBookService = addBookService;
        }

        // STEP 1: Preview book info from ISBN (no event)
        [HttpPost("preview")]
        public async Task<IActionResult> PreviewBookAsync([FromBody] IsbnPreviewRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Isbn))
                return BadRequest("ISBN is required");

            var book = await _addBookService.PreviewBookAsync(request.Isbn);
            return Ok(book);
        }


        // STEP 2: Final creation with condition/price/quantity
        [HttpPost]
        public async Task<IActionResult> CreateBookAsync([FromBody] AddBookRequest request)
        {
            // Log all headers for debugging
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AddBookController>>();
            logger.LogInformation("=== AddBook Request Headers ===");
            foreach (var header in Request.Headers)
            {
                logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
            }
            
            // Extract UserId from JWT claims sent by Gateway
            var userIdClaim = Request.Headers["X-User-Id"].FirstOrDefault();
            
            logger.LogInformation("X-User-Id header value: {UserIdClaim}", userIdClaim ?? "(null)");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("User ID not found or invalid in X-User-Id header");
                return Unauthorized("User ID not found in request");
            }
            
            logger.LogInformation("User ID successfully extracted: {UserId}", userId);
            
            request.UserId = userId;
            
            var result = await _addBookService.AddBookAsync(request);
            return Ok(result);
        }
    }
}