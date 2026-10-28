using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ForFitChicken.API.Authentication;
using ForFitChicken.API.RateLimiting;
using ForFitChicken.Application.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ForFitChicken.API.Controllers;

[ApiController]
[Route("api/admin/auth")]
[Authorize(Policy = AdminPolicies.Staff)]
[EnableRateLimiting(RateLimitPolicies.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAuthController(IAdminAuthService auth) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CsrfTokenResponse), StatusCodes.Status200OK)]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery) =>
        Ok(new CsrfTokenResponse(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType(typeof(AdminProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!await auth.LoginAsync(request.UserName, request.Password))
            return AuthProblem(401, "LOGIN_FAILED", "Không thể đăng nhập. Kiểm tra thông tin hoặc thử lại sau.");
        return Ok(await auth.GetProfileAsync(UserId()));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AdminProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var profile = await auth.GetProfileAsync(UserId());
        return profile is null ? AuthProblem(401, "AUTH_REQUIRED", "Bạn cần đăng nhập.") : Ok(profile);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Logout()
    {
        if (!await auth.RevokeSessionsAsync(UserId()))
            return AuthProblem(409, "SESSION_REVOKE_FAILED", "Chưa thể thu hồi phiên. Hãy thử lại.");
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private ObjectResult AuthProblem(int status, string code, string title) => new(new ProblemDetails
    {
        Status = status, Title = title, Extensions = { ["code"] = code }
    }) { StatusCode = status, ContentTypes = { "application/problem+json" } };
}

public sealed record CsrfTokenResponse(string RequestToken);

public sealed class LoginRequest
{
    [Required, StringLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
