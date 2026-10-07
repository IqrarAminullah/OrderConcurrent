using Microsoft.AspNetCore.Http.HttpResults;
using OrderConcurrent.Data.Interfaces;
using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;
using OrderConcurrent.Repository.Interfaces;
using OrderConcurrent.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;


namespace OrderConcurrent.Services
{
    public class OrderService : IOrderService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        private readonly IDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;
        public OrderService(IProductRepository productRepository, IOrderRepository orderRepository, IDbContext dbContext, ILogger<OrderService> logger)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Pagination<Order>> GetListOrder(OrderFilter filter, int page, int pageSize)
            => await _orderRepository.GetListOrder(filter, page, pageSize);

        public async Task<Order> GetOrder(int orderId)
        {
            _logger.LogInformation($"Fetching order with ID {orderId}.");
            var order = await _orderRepository.GetOrder(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Order with ID {orderId} not found.");
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }
            return order;
        }

        public async Task<Order> CreateOrder(Order order)
        {
            _dbContext.BeginTransaction();
            try
            {
                _logger.LogInformation($"Creating order for customer {order.CustomerId} with {order.OrderItems.Count} items.");
                List<OrderItem> productStock = await _productRepository.GetProductStock(order.OrderItems);
                var createdId = await _orderRepository.CreateOrder(order, productStock);
                order.OrderId = createdId;
                if (order.Status != StatusEnum.Cancelled)
                {
                    await _productRepository.UpdateProductStock(order.OrderItems, order.Status);
                }
                _dbContext.Commit();
                return await _orderRepository.GetOrder(createdId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order.");
                _dbContext.Rollback();
                throw;
            }
        }

        public async Task<Order> UpdateOrderStatus(int orderId, StatusEnum status)
        {
            _dbContext.BeginTransaction();
            try
            {
                var updatedOrder = await _orderRepository.UpdateOrderStatus(orderId, status);
                if (status == StatusEnum.Cancelled && updatedOrder != null)
                {
                    await _productRepository.UpdateProductStock(updatedOrder.OrderItems, updatedOrder.Status);
                }
                _dbContext.Commit();
                return updatedOrder;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating order status for order ID {orderId}.");
                _dbContext.Rollback();
                return null;
            }
        }
    }
}
