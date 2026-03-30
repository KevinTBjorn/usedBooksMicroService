using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Domain.Dto;

namespace WarehouseService.Controllers
{
    [ApiController]
    [Route("warehouse")]
    public class WarehouseApiController : ControllerBase
    {
        private readonly Domain.IWarehouseService _svc;

        public WarehouseApiController(Domain.IWarehouseService svc)
        {
            _svc = svc;
        }

        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var list = await _svc.GetAllBooksAsync();
            return Ok(list);
        }

        [HttpGet("books/{bookId}/inventory")]
        public async Task<IActionResult> GetInventory([FromRoute] Guid bookId)
        {
            var dto = await _svc.GetInventoryForBookAsync(bookId);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // Get all books with pagination { store/home } 
        [HttpGet("bookspaginationnew")]
        public async Task<IActionResult> GetBooks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var books = await _svc.GetBooksAsync(pageNumber, pageSize);
            return Ok(books);
        }

        // Get all UserBooks by BookId/ISBN { store/bookID } 
        [HttpGet("userbooks/{bookId}")]
        public async Task<IActionResult> GetUserBooksByBookId([FromRoute] Guid bookId)
        {
            var userBooks = await _svc.GetUserBooksByBookIdAsync(bookId);
            return Ok(userBooks);
        }

        // Get all UserBooks by UserID { user/inventory }
        [HttpGet("userbooks/user/{userId}")]
        public async Task<IActionResult> GetUserBooksByUserId([FromRoute] Guid userId)
        {
            var userBooks = await _svc.GetUserBooksByUserIdAsync(userId);
            return Ok(userBooks);
        }
    }
}
