using MicroclimateIotSystem.WebAPI.Abstractions;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
                       .WithTags("Authentication");

        group.MapPost("/login", LoginAsync).WithName("Login").WithOpenApi();
        group.MapPost("/register", RegisterAsync).WithName("Register").WithOpenApi();
        group.MapPost("/refresh", RefreshTokenAsync).WithName("RefreshToken").WithOpenApi();
        group.MapPost("/signout", SignoutAsync).WithName("Signout").WithOpenApi();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequestDto request,
        IAuthService authService,
        IOptions<JwtConfig> jwtConfig,
        HttpContext httpContext)
    {
        var response = await authService.LoginAsync(request);
        
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(httpContext.Response, response.Data.RefreshToken, jwtConfig.Value.RefreshTokenExpirationInMinutes);
        }

        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequestDto request,
        IAuthService authService,
        IOptions<JwtConfig> jwtConfig,
        HttpContext httpContext)
    {
        var response = await authService.RegisterAsync(request);
        
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(httpContext.Response, response.Data.RefreshToken, jwtConfig.Value.RefreshTokenExpirationInMinutes);
        }

        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> RefreshTokenAsync(
        IAuthService authService,
        IOptions<JwtConfig> jwtConfig,
        HttpContext httpContext)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return TypedResults.Json(StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, null, "Refresh token is missing."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var response = await authService.RefreshTokenAsync(new RefreshTokenRequestDto(refreshToken));
        
        if (response.Success && response.Data != null)
        {
            SetRefreshTokenCookie(httpContext.Response, response.Data.RefreshToken, jwtConfig.Value.RefreshTokenExpirationInMinutes);
        }

        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> SignoutAsync(
        IAuthService authService,
        HttpContext httpContext)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return TypedResults.Json(StandardResponse<bool>.Create(ResultStatus.Unauthorized, false, "Refresh token is missing."), statusCode: StatusCodes.Status401Unauthorized);
        }

        var response = await authService.SignOutAsync(new RefreshTokenRequestDto(refreshToken));
        
        if (response.Success)
        {
            httpContext.Response.Cookies.Delete("refreshToken");
        }

        return ResultHandler.Handle(response);
    }

    private static void SetRefreshTokenCookie(HttpResponse response, string token, int expirationInMinutes)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
            SameSite = SameSiteMode.Strict,
            Secure = true
        };
        response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
