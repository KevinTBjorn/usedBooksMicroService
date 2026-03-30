using System;

namespace Domain.Events
{
    public class WarehouseOrderValidatedMessage
    {
        public Guid OrderId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime ValidatedAt { get; set; }
    }
}
