using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.DTOs.Auth;
using OrderFlow.Application.UseCases.Auth;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterUserUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new { userId = result.Value });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginUserUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
