using System;
using System.Collections.Generic;

namespace Domain.Events
{
    public class OrderValidationRequestMessage
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public Guid CustomerId { get; set; }
        public List<OrderItem> Items { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItem
    {
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; } = default!;
        public decimal Price { get; set; }
    }
}
