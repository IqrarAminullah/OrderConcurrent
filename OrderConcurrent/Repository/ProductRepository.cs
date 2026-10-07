using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderConcurrent.Data.Interfaces;
using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;
using OrderConcurrent.Repository.Interfaces;
using OrderConcurrent.Services;
using OrderConcurrent.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OrderConcurrent.Repository
{
    public class ProductRepository : IProductRepository
    {

        private IDbContext _dbContext;
        private ILogger<ProductRepository> _logger;
        public ProductRepository(IDbContext dbContext, ILogger<ProductRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public Task<int> CreateProduct(Product product)
        {
            throw new NotImplementedException();
        }

        public async Task<Product> GetProduct(int productId)
        {
            try
            {
                var result = await _dbContext.Connection.QueryAsync<Product>("SELECT * FROM Products WHERE ProductId = @ProductId",
                    new { ProductId = productId.ToString() }, transaction: _dbContext.Transaction);
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product.");
                return null;
            }
        }

        public async Task<List<OrderItem>> GetProductStock(List<OrderItem> orderItems)
        {
            try
            {
                var result = await _dbContext.Connection.QueryAsync<OrderItem>(
                    "SELECT ProductId, Quantity FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId IN @ProductIds",
                    new { ProductIds = orderItems.Select(oi => oi.ProductId).ToArray() }, transaction: _dbContext.Transaction);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product stock.");
                return new List<OrderItem>();
            }
        }

        public Task<List<Product>> GetListProducts()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateProduct(Product product)
        {

            try {
                const string query =
                "UPDATE Products SET ProductName = @ProductName, " +
                "ProductQuantity = @Quantity, " +
                "ProductPrice = @Price " +
                "WHERE ProductId = @ProductId";
                int rows = await _dbContext.Connection.ExecuteAsync(query, product, transaction: _dbContext.Transaction);
                return rows == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product.");
                return false;
            }
        }

        public async Task<bool> UpdateProductStock(List<OrderItem> orderItems, StatusEnum status)
        {
            try
            {
                int mod = 1;
                if (status == StatusEnum.Cancelled)
                {
                    mod = -1;
                }
                foreach (var item in orderItems)
                {
                    const string query =
                        "UPDATE Products SET Quantity = Quantity - @Quantity WHERE ProductId = @ProductId";
                    int rows = await _dbContext.Connection.ExecuteAsync(query, new { Quantity = item.Quantity * mod, ProductId = item.ProductId }, transaction: _dbContext.Transaction);
                    if (rows != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product stock.");
                return false;
            }
        }

        public async Task<bool> DeleteProduct(int productId)
        {
            const string query =
                    "DELETE FROM Products " +
                    "WHERE ProductId = @ProductId";
            int rows = await _dbContext.Connection.ExecuteAsync(query, new { ProductId = productId.ToString() }, transaction: _dbContext.Transaction);
            return rows == 1;
        }
    }
}
