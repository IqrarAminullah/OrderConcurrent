using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;

namespace OrderConcurrent.Repository.Interfaces
{
    public interface IProductRepository
    {
        public Task<int> CreateProduct(Product product);
        public Task<Product> GetProduct(int productId);
        public Task<List<OrderItem>> GetProductStock(List<OrderItem> orderItems);
        public Task<List<Product>> GetListProducts();
        public Task<bool> UpdateProduct(Product product);
        public Task<bool> UpdateProductStock(List<OrderItem> orderItems, StatusEnum status);
        public Task<bool> DeleteProduct(int productId);
    }
}
