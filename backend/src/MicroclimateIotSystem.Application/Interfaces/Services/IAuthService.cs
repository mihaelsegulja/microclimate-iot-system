using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Models;

namespace MicroclimateIotSystem.Application.Interfaces.Services;

public interface IAuthService
{
    Task<StandardResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<StandardResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<StandardResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<StandardResponse<bool>> SignOutAsync(RefreshTokenRequestDto request);
}

