namespace OrderService.Models
{
    public class UserBook
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        public Guid BookId { get; set; } = default!;

        public int Quantity { get; set; }

        public string Condition { get; set; } = default!;

        public decimal Price { get; set; }
    }
}
