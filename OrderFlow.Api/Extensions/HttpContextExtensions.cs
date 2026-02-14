using OrderFlow.Api.Middleware;

namespace OrderFlow.Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) &&
            value is Guid guid && guid != Guid.Empty)
            return guid;

        return Guid.NewGuid();
    }
}
