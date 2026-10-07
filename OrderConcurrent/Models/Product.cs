namespace OrderConcurrent.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; } = 0m;
        public int Quantity { get; set; }
    }
}
