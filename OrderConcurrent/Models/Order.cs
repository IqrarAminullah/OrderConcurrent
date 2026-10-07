using OrderConcurrent.Models.Enums;

namespace OrderConcurrent.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string? CustomerId { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public string? ShippingAddress { get; set; }
        public StatusEnum Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdateAt { get; set; }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
