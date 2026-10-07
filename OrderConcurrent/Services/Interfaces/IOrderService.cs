using OrderConcurrent.Repository.Interfaces;
using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;

namespace OrderConcurrent.Services.Interfaces
{
    public interface IOrderService
    {
        public Task<Pagination<Order>> GetListOrder(OrderFilter filter, int page, int pageSize);
        public Task<Order> GetOrder(int orderId);
        public Task<Order> CreateOrder(Order order);

        public Task<Order> UpdateOrderStatus(int orderId, StatusEnum status);
    }
}
