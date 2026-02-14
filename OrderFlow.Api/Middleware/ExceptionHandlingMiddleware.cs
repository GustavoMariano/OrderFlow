using System.Net;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error");
            await WriteProblem(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            await WriteProblem(context, HttpStatusCode.InternalServerError, "Unexpected error.");
        }
    }

    private static async Task WriteProblem(HttpContext ctx, HttpStatusCode code, string detail)
    {
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/problem+json";

        var payload = new
        {
            title = code == HttpStatusCode.BadRequest ? "Bad Request" : "Server Error",
            status = (int)code,
            detail
        };

        await ctx.Response.WriteAsJsonAsync(payload);
    }
}
