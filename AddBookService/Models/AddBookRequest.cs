using System.ComponentModel.DataAnnotations;

namespace AddBookService.Models
{
    public class AddBookRequest
    {
        public Guid UserId { get; set; }    
        public string Isbn { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
    }
}