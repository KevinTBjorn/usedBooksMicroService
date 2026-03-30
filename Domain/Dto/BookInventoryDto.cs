using System;
using System.Collections.Generic;

namespace Domain.Dto
{
    public class BookInventoryDto
    {
        public Guid BookId { get; set; }
        public int TotalQuantity { get; set; }
        public List<UserBookListingDto> Listings { get; set; } = new();
    }
}
