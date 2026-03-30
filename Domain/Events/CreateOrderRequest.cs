using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events
{
    public class CreateOrderRequest
    {
        public Guid UserId { get; set; }
        public Guid CustomerId { get; set; }
        public List<CreateOrderItem> Items { get; set; } = new();
    }
    public class CreateOrderItem
    {
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; } = default!;
        public decimal Price { get; set; }
    }
}
