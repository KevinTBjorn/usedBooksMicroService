namespace OrderService.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CorrelationId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string? RejectionReasons { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        public List<UserBook> Items { get; set; } = new();
    }

    public enum OrderStatus
    {
        Pending,
        Validated,
        Rejected
    }
}
