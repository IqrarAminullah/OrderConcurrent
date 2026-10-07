using System.Collections.Concurrent;

namespace OrderConcurrent.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, string> _processedRequests = new();

        public IdempotencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to POST requests
            if (context.Request.Method != HttpMethods.Post)
            {
                await _next(context);
                return;
            }

            // Extract the header
            if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing Idempotency-Key header");
                return;
            }

            // Check if key already used
            if (_processedRequests.ContainsKey(key))
            {
                context.Response.StatusCode = 409; // Conflict
                await context.Response.WriteAsync("Duplicate request detected");
                return;
            }

            // Process the request
            _processedRequests[key] = context.TraceIdentifier;
            await _next(context);
        }
    }
}
