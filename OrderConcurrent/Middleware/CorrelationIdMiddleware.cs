
namespace OrderConcurrent.Middleware
{
    public interface ICorrelationIdAccessor
    {
        string CorrelationId { get; set; }
    }
}

namespace OrderConcurrent.Middleware
{
    public class CorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId { get; set; } = string.Empty;
    }
}

namespace OrderConcurrent.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
        {
            // Try to get correlation ID from incoming request
            var correlationId = GetOrCreateCorrelationId(context);

            // Store it for the duration of the request
            accessor.CorrelationId = correlationId;

            // Add to response headers so clients can see it
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
                return Task.CompletedTask;
            });

            // Add to the HttpContext.Items for easy access
            context.Items[CorrelationIdHeader] = correlationId;

            await _next(context);
        }

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            // Check if the request already has a correlation ID
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
                && !string.IsNullOrWhiteSpace(existingId))
            {
                return existingId.ToString();
            }

            // Generate a new one if not present
            return Guid.NewGuid().ToString("N");
        }
    }
}
