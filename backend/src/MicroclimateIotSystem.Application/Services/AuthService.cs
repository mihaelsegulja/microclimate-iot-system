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
    IOptions<JwtOptions> jwtOptions)
    : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<StandardResponse<AuthResponseDto>> LoginAsync(
        LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user == null || !passwordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid username or password.");

        var accessToken = tokenHelper.GenerateAccessToken(user);
        var refreshToken = tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Login successful.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

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
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenHelper.GenerateAccessToken(newUser);
        var refreshToken = tokenHelper.GenerateRefreshToken();

        newUser.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Created, responseData, "User registered successfully.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken == null || existingToken.IsExpired || existingToken.Revoked != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid or expired refresh token.");

        var user = existingToken.User;

        existingToken.Revoked = DateTime.UtcNow;

        var newAccessToken = tokenHelper.GenerateAccessToken(user);
        var newRefreshToken = tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        var responseData = new AuthResponseDto(newAccessToken, newRefreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Token refreshed successfully.");
    }

    public async Task<StandardResponse<bool>> SignOutAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken == null)
            return StandardResponse<bool>.Create(ResultStatus.NotFound, false, "Token not found.");

        if (existingToken.Revoked != null)
            return StandardResponse<bool>.Create(ResultStatus.Conflict, false, "Token is already revoked.");

        existingToken.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.Create(ResultStatus.Ok, true, "Signed out successfully.");
    }
}
