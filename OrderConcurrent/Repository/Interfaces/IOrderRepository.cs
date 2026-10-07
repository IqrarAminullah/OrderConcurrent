using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;

namespace OrderConcurrent.Repository.Interfaces
{
    public interface IOrderRepository
    {
        public Task<int> CreateOrder(Order order, List<OrderItem> stock);
        public Task<Order> GetOrder(int orderId);
        public Task<Pagination<Order>> GetListOrder(OrderFilter filter, int page, int pageSize);
        public Task<bool> UpdateOrder(Order order);
        public Task<Order> UpdateOrderStatus(int orderId, StatusEnum status);
    }
}
