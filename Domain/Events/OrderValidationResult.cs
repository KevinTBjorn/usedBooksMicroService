using System.Collections.Generic;

namespace Domain.Events
{
    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
    }
}
