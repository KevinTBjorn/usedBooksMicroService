namespace WarehouseService.Models
{
    public class UserBook
    {
        public Guid BookId { get; private set; }
        public Guid UserId { get; private set; }

        public string Condition { get; private set; }
        public int Quantity { get; set; }
        public decimal Price { get; private set; }

        public Book Book { get; private set; }

        private UserBook() { }

        public UserBook(Guid bookId, Guid userId, string condition, int quantity, decimal price)
        {
            BookId = bookId;
            UserId = userId;
            Condition = condition;
            Quantity = quantity;
            Price = price;
        }
    }
}
