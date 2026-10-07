using Serilog.Context;

namespace OrderConcurrent.Middleware
{
    public class CorrelationIdLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
        {
            // Push correlation ID to Serilog's log context
            using (LogContext.PushProperty("CorrelationId", accessor.CorrelationId))
            {
                await _next(context);
            }
        }
    }
}
