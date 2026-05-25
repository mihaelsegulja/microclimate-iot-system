using MicroclimateIotSystem.WebAPI.Abstractions.Controllers;
using MicroclimateIotSystem.Application.DTOs.Auth;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MicroclimateIotSystem.WebAPI.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly JwtConfig _jwtConfig;

    public AuthController(IAuthService authService, IOptions<JwtConfig> jwtConfig)
    {
        _authService = authService;
        _jwtConfig = jwtConfig.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(response.Data.RefreshToken);
        }
        return HandleResponse(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(response.Data.RefreshToken);
        }
        return HandleResponse(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var response = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = refreshToken });
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(response.Data.RefreshToken);
        }
        return HandleResponse(response);
    }

    [HttpPost("signout")]
    public async Task<IActionResult> Signout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "Refresh token is missing." });
        }

        var response = await _authService.SignOutAsync(new RefreshTokenRequestDto { RefreshToken = refreshToken });
        if (response.Success)
        {
            Response.Cookies.Delete("refreshToken");
        }
        return HandleResponse(response);
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            SameSite = SameSiteMode.Strict,
            Secure = true
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
