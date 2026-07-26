using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MicroclimateIotSystem.Application.Services;

public class AuthService(
    IAppDbContext db,
    IPasswordHelper passwordHelper,
    ITokenHelper tokenHelper,
    IOptions<JwtConfig> jwtConfig)
    : IAuthService
{
    private readonly JwtConfig _jwtConfig = jwtConfig.Value;

    public async Task<StandardResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !passwordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid username or password.");

        var accessToken = tokenHelper.GenerateAccessToken(user);
        var refreshToken = tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Login successful.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (existingUser != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Conflict, message: "User already exists.");

        var salt = passwordHelper.GenerateSalt();
        var hash = passwordHelper.HashPassword(request.Password, salt);

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var accessToken = tokenHelper.GenerateAccessToken(newUser);
        var refreshToken = tokenHelper.GenerateRefreshToken();

        newUser.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Created, responseData, "User registered successfully.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken == null || existingToken.IsExpired || existingToken.Revoked != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid or expired refresh token.");

        var user = existingToken.User;

        existingToken.Revoked = DateTime.UtcNow;

        var newAccessToken = tokenHelper.GenerateAccessToken(user);
        var newRefreshToken = tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var responseData = new AuthResponseDto(newAccessToken, newRefreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Token refreshed successfully.");
    }

    public async Task<StandardResponse<bool>> SignOutAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken == null)
            return StandardResponse<bool>.Create(ResultStatus.NotFound, false, "Token not found.");

        if (existingToken.Revoked != null)
            return StandardResponse<bool>.Create(ResultStatus.Conflict, false, "Token is already revoked.");

        existingToken.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return StandardResponse<bool>.Create(ResultStatus.Ok, true, "Signed out successfully.");
    }
}
