using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Extensions;
using OrderFlow.Application.DTOs.Orders;
using OrderFlow.Application.UseCases.Orders;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromServices] CreateOrderUseCase useCase,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var correlationId = HttpContext.GetCorrelationId();

        var result = await useCase.ExecuteAsync(userId, request, correlationId, ct);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Accepted(new { orderId = result.Value, correlationId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromServices] GetOrdersUseCase useCase,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await useCase.ExecuteAsync(userId, ct);

        return result.IsFailure ? BadRequest(result.Error) : Ok(result.Value);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid orderId,
        [FromServices] GetOrderByIdUseCase useCase,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await useCase.ExecuteAsync(userId, orderId, ct);

        return result.IsFailure ? NotFound(result.Error) : Ok(result.Value);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
