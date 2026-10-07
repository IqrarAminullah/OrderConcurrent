using OrderConcurrent.Models.Enums;

namespace OrderConcurrent.Models
{
    public class OrderFilter
    {
        public StatusEnum? Status { get; set; }
        public string? CustomerId { get; set; }
        public DateTime? Before { get; set; }
        public DateTime? After { get; set; }
    }
}
