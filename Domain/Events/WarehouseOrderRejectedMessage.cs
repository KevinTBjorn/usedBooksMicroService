using System;
using System.Collections.Generic;

namespace Domain.Events
{
    public class WarehouseOrderRejectedMessage
    {
        public Guid OrderId { get; set; }
        public Guid CorrelationId { get; set; }
        public List<string> Reasons { get; set; }
        public DateTime RejectedAt { get; set; }
    }
}
