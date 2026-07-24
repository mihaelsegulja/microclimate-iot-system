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

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHelper _passwordHelper;
    private readonly ITokenHelper _tokenHelper;
    private readonly JwtConfig _jwtConfig;

    public AuthService(
        IAppDbContext db,
        IPasswordHelper passwordHelper,
        ITokenHelper tokenHelper,
        IOptions<JwtConfig> jwtConfig)
    {
        _db = db;
        _passwordHelper = passwordHelper;
        _tokenHelper = tokenHelper;
        _jwtConfig = jwtConfig.Value;
    }

    public async Task<StandardResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !_passwordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid username or password.");

        var accessToken = _tokenHelper.GenerateAccessToken(user);
        var refreshToken = _tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Login successful.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (existingUser != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Conflict, message: "User already exists.");

        var salt = _passwordHelper.GenerateSalt();
        var hash = _passwordHelper.HashPassword(request.Password, salt);

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        var accessToken = _tokenHelper.GenerateAccessToken(newUser);
        var refreshToken = _tokenHelper.GenerateRefreshToken();

        newUser.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var responseData = new AuthResponseDto(accessToken, refreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Created, responseData, "User registered successfully.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken == null || existingToken.IsExpired || existingToken.Revoked != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid or expired refresh token.");

        var user = existingToken.User;

        existingToken.Revoked = DateTime.UtcNow;

        var newAccessToken = _tokenHelper.GenerateAccessToken(user);
        var newRefreshToken = _tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var responseData = new AuthResponseDto(newAccessToken, newRefreshToken);

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Token refreshed successfully.");
    }

    public async Task<StandardResponse<bool>> SignOutAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken == null)
            return StandardResponse<bool>.Create(ResultStatus.NotFound, false, "Token not found.");

        if (existingToken.Revoked != null)
            return StandardResponse<bool>.Create(ResultStatus.Conflict, false, "Token is already revoked.");

        existingToken.Revoked = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return StandardResponse<bool>.Create(ResultStatus.Ok, true, "Signed out successfully.");
    }
}
