using MicroclimateIotSystem.Application.DTOs.Auth;
using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Domain.Interfaces;
using MicroclimateIotSystem.Application.Configurations;
using Microsoft.Extensions.Options;

namespace MicroclimateIotSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHelper _passwordHelper;
    private readonly ITokenHelper _tokenHelper;
    private readonly JwtConfig _jwtConfig;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHelper passwordHelper,
        ITokenHelper tokenHelper,
        IOptions<JwtConfig> jwtConfig)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHelper = passwordHelper;
        _tokenHelper = tokenHelper;
        _jwtConfig = jwtConfig.Value;
    }

    public async Task<StandardResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
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

        await _userRepository.SaveChangesAsync();

        var responseData = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Login successful.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        
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

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();
        
        var accessToken = _tokenHelper.GenerateAccessToken(newUser);
        var refreshToken = _tokenHelper.GenerateRefreshToken();

        newUser.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        await _userRepository.SaveChangesAsync();

        var responseData = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Created, responseData, "User registered successfully.");
    }

    public async Task<StandardResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (existingToken == null || existingToken.IsExpired || existingToken.Revoked != null)
            return StandardResponse<AuthResponseDto>.Create(ResultStatus.Unauthorized, message: "Invalid or expired refresh token.");

        var user = existingToken.User;
        
        // Complete current token lifecycle
        existingToken.Revoked = DateTime.UtcNow;
        _refreshTokenRepository.Update(existingToken);

        var newAccessToken = _tokenHelper.GenerateAccessToken(user);
        var newRefreshToken = _tokenHelper.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpirationInMinutes),
            Created = DateTime.UtcNow
        });

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        var responseData = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return StandardResponse<AuthResponseDto>.Create(ResultStatus.Ok, responseData, "Token refreshed successfully.");
    }

    public async Task<StandardResponse<bool>> SignOutAsync(RefreshTokenRequestDto request)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (existingToken == null)
            return StandardResponse<bool>.Create(ResultStatus.NotFound, false, "Token not found.");

        if (existingToken.Revoked != null)
            return StandardResponse<bool>.Create(ResultStatus.Conflict, false, "Token is already revoked.");

        existingToken.Revoked = DateTime.UtcNow;
        _refreshTokenRepository.Update(existingToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return StandardResponse<bool>.Create(ResultStatus.Ok, true, "Signed out successfully.");
    }
}

