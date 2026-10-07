using Dapper;
using Microsoft.Data.SqlClient;
using OrderConcurrent.Data.Interfaces;
using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;
using OrderConcurrent.Repository.Interfaces;
using OrderConcurrent.Services;
using OrderConcurrent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace OrderConcurrent.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private IDbContext _dbContext;
        private ILogger<OrderRepository> _logger;
        public OrderRepository(IDbContext dbConnection, ILogger<OrderRepository> logger)
        {
            _dbContext = dbConnection;
            _logger = logger;
        }
        public async Task<int> CreateOrder(Order order, List<OrderItem> stock)
        {
            try
            {
                foreach (var item in order.OrderItems)
                {
                    var stockItem = stock.FirstOrDefault(s => s.ProductId == item.ProductId);
                    if (stockItem == null || stockItem.Quantity < item.Quantity)
                    {
                        throw new Exception($"Not enough stock for product {item.ProductId}");
                    }
                }
                const string insertOrderQuery = "INSERT INTO Orders (CustomerId, ShippingAddress, Status, CreatedAt, LastUpdateAt)" +
                        " VALUES (@CustomerId, @ShippingAddress, @Status, @CreatedAt, @LastUpdateAt);" +
                        " SELECT CAST(SCOPE_IDENTITY() AS INT)";
                var orderId = await _dbContext.Connection.ExecuteScalarAsync<int>(insertOrderQuery, order, transaction: _dbContext.Transaction);

                const string insertOrderItemsQuery = "INSERT INTO ProductOrders (OrderId, ProductId, Quantity)" +
                    " VALUES (@OrderId, @ProductId, @Quantity);";
                var orderItems = order.OrderItems.Select(item => new
                {
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList();

                await _dbContext.Connection.ExecuteAsync(insertOrderItemsQuery, orderItems, transaction: _dbContext.Transaction);
                return orderId;
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order.");
                return -1;
            }
        }

        public async Task<Order> GetOrder(int orderId)
        {
            try 
            {
                Order? order = null;
                const string multiQuery =
                    "SELECT OrderId, CustomerId, ShippingAddress, Status FROM Orders WHERE OrderId = @OrderId;\n" +
                    "SELECT ProductId, Quantity FROM ProductOrders WHERE OrderId = @OrderId;";
                using (var results = await _dbContext.Connection.QueryMultipleAsync(multiQuery,
                    new { OrderId = orderId },
                    transaction: _dbContext.Transaction))
                {
                    order = results.ReadFirstOrDefault<Order>();
                    if (order != null)
                    {
                        var orderItem = results.Read<OrderItem>().ToList();
                        order.OrderItems = orderItem;
                    }
                }
                return order;
            } catch (Exception ex)
            { 
                _logger.LogError(ex, "Error occurred while retrieving order.");
                return null;
            }
        }

        public async Task<Pagination<Order>> GetListOrder(OrderFilter filter, int page, int pageSize)
        {
            try
            {
                var sqlFilter = "";
                if (filter.Status != null || !string.IsNullOrEmpty(filter.CustomerId) || filter.Before != null || filter.After != null)
                {
                    sqlFilter = "\n WHERE";
                }
                if (filter.Status != null)
                {
                    sqlFilter += "o.Status = @Status";
                }
                if (!string.IsNullOrEmpty(filter.CustomerId))
                {
                    sqlFilter += filter.Status != null ? " AND o.CustomerId = @CustomerId" : " WHERE o.CustomerId = @CustomerId";
                }
                if (filter.Before != null)
                {
                    sqlFilter += filter.Status != null || !string.IsNullOrEmpty(filter.CustomerId) ? " AND o.CreatedAt <= @Before" : " WHERE o.CreatedAt <= @Before";
                }
                if (filter.After != null)
                {
                    sqlFilter += filter.Status != null || !string.IsNullOrEmpty(filter.CustomerId) || filter.Before != null ? " AND o.CreatedAt >= @After" : " WHERE o.CreatedAt >= @After";
                }
                var sqlList = @"
                SELECT 
                    o.OrderId, o.CustomerId, o.ShippingAddress, o.Status,
                    po.OrderId, po.ProductId, po.Quantity
                FROM Orders o
                LEFT JOIN ProductOrders po ON o.OrderId = po.OrderId";

                sqlList += sqlFilter + "\n ORDER BY o.OrderId OFFSET @CurrentPage ROWS FETCH NEXT @PageSize ROWS ONLY";

                var orders = await _dbContext.Connection.QueryAsync<Order, OrderItem, Order>(
                    sqlList,
                    (order, orderItem) =>
                    {
                        order.OrderItems.Add(orderItem);
                        return order;
                    },
                    param: new
                    {
                        Status = filter.Status,
                        CustomerId = filter.CustomerId,
                        Before = filter.Before,
                        After = filter.After,
                        CurrentPage = (page - 1) * pageSize,
                        PageSize = pageSize
                    },
                    transaction: _dbContext.Transaction,
                    splitOn: "OrderId");

                var result = orders.GroupBy(o => o.OrderId).Select(g =>
                {
                    var groupedOrder = g.First();
                    groupedOrder.OrderItems = g.Select(item => item.OrderItems.FirstOrDefault()).Where(item => item != null).ToList();
                    return groupedOrder;
                });

                var sqlCount = @"
                SELECT COUNT(*) 
                FROM Orders o" + sqlFilter;
                var totalCount = await _dbContext.Connection.QuerySingleAsync<int>(sqlCount, new
                {
                    Status = filter.Status,
                    CustomerId = filter.CustomerId,
                    Before = filter.Before,
                    After = filter.After
                }, transaction: _dbContext.Transaction);
                return new Pagination<Order>(result.ToList(), totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving order list.");
                return new Pagination<Order>(new List<Order>(), 0, page, pageSize);
            }
        }
        public async Task<bool> UpdateOrder(Order order)
        {
            try
            {
                const string query =
                    "UPDATE Orders SET " +
                    "CustomerId = @CustomerId," +
                    "ShippingAddress = @ShippingAddress," +
                    "Status = @Status" +
                    "WHERE Id = @orderId";
                int rows = await _dbContext.Connection.ExecuteAsync(query, order, transaction: _dbContext.Transaction);
                return rows == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order.");
                return false;
            }
        }

        private bool IsValidStatusTransition(StatusEnum currentStatus, StatusEnum newStatus)
        {
            return currentStatus switch
            {
                StatusEnum.Pending => newStatus == StatusEnum.Confirmed || newStatus == StatusEnum.Cancelled,
                StatusEnum.Confirmed => newStatus == StatusEnum.Shipped || newStatus == StatusEnum.Cancelled,
                StatusEnum.Shipped => newStatus == StatusEnum.Delivered,
                _ => false,
            };
        }
        public async Task<Order> UpdateOrderStatus(int orderId, StatusEnum status)
        {
            try
            { 
                const string orderQuery =
                    "SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK) WHERE OrderId = @OrderId";
                var order = await _dbContext.Connection.QuerySingleAsync<Order>(orderQuery, new { OrderId = orderId }, transaction: _dbContext.Transaction);
                
                if (!IsValidStatusTransition(order.Status, status))
                {
                    throw new InvalidOperationException("Invalid status transition");
                }

                const string updateQuery =
                    "UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId";
                int rows = await _dbContext.Connection.ExecuteAsync(updateQuery, new { OrderId = orderId, Status = status }, transaction: _dbContext.Transaction);
               
                if(rows == 1) 
                {
                    order.Status = status;
                }
                return order;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred while updating order status.");
                return null;
            }
        }
    }

}
