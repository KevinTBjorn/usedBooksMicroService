using Domain;
using Microsoft.AspNetCore.Mvc;
using SearchService.Repositories;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SearchService.Models;

namespace SearchService.Controllers
{
    [ApiController]
    [Route("search")]
    public class SearchApiController : ControllerBase
    {
        private readonly ISearchRepository _repo;

        public SearchApiController(ISearchRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var results = await _repo.SearchAsync(query ?? string.Empty);
            return Ok(results);
        }

        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetBook([FromRoute] Guid bookId)
        {
            var book = await _repo.GetBookMetadataAsync(bookId);
            if (book == null) return NotFound();
            return Ok(book);
        }
    }
}
