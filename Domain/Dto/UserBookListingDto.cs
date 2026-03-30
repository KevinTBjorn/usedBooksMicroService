using System;

namespace Domain.Dto
{
    public class UserBookListingDto
    {
        public Guid BookId { get; set; }
        public Guid UserId { get; set; }
        public string Condition { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
