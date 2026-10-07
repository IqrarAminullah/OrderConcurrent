using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderConcurrent.Middleware;
using OrderConcurrent.Models;
using OrderConcurrent.Models.Enums;
using OrderConcurrent.Repository.Interfaces;
using OrderConcurrent.Services;
using OrderConcurrent.Services.Interfaces;
using System.Data;

namespace OrderConcurrent.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderService _orderService;
        private readonly ICorrelationIdAccessor _correlationId;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService, ICorrelationIdAccessor correlationId)
        {
            _logger = logger;
            _orderService = orderService;
            _correlationId = correlationId;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pagination<Order>>>> GetOrders([FromQuery] OrderFilter filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var Orders = await _orderService.GetListOrder(filter, page, pageSize);
            return Ok(Orders);
        }

        [HttpPost("GetOrder")]
        public async Task<ActionResult<Order>> GetOrder([FromBody] int id)
        {
            _logger.LogInformation("Get Order Id : {OrderId}, correlation ID: {CorrelationId}", id, _correlationId.CorrelationId);
            var Order = await _orderService.GetOrder(id);
            if (Order == null)
            {
                return NotFound(new ApiResponse<string>(404, "Order not found", null));
            }
            return Ok(new ApiResponse<Order>(200, "Success", Order));
        }

        [HttpPost("CreateOrder")]
        public async Task<ActionResult<Order>> CreateOrder([FromBody]Order order)
        {
            _logger.LogInformation("Create Order : {Order}, correlation ID: {CorrelationId}", order.ToString(), _correlationId.CorrelationId);
            try
            {
                var createdOrder = await _orderService.CreateOrder(order);
                if (createdOrder != null)
                {
                    return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.OrderId }, new ApiResponse<Order>(201, "Success", createdOrder));
                }
                return BadRequest(new ApiResponse<string>(400, "Failed to create order", null));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}, correlation ID: {CorrelationId}", ex.Message, _correlationId.CorrelationId);
                return BadRequest(new ApiResponse<string>(400, ex.Message, null));
            }
        }

        [HttpPost("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus(int id, StatusEnum status)
        {
            _logger.LogInformation("Update Order Status : {OrderId}, New Status: {Status}, correlation ID: {CorrelationId}", id, status, _correlationId.CorrelationId);
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatus(id, status);
                if (updatedOrder == null)
                {
                    return NotFound(new ApiResponse<string>(404, "Order not found", null));
                }
                return Ok(new ApiResponse<Order>(200, "Success", updatedOrder));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error updating order status: {Message}, correlation ID: {CorrelationId}", ex.Message, _correlationId.CorrelationId);
                return BadRequest(new ApiResponse<string>(400, ex.Message, null));
            }
        }
    }
}
