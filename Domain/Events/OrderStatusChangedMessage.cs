namespace Domain.Events
{
    public class OrderStatusValidatedMessage
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
