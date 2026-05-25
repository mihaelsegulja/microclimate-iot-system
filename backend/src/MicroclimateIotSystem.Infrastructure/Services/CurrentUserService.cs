using MicroclimateIotSystem.Application.Constants;
using MicroclimateIotSystem.Application.Interfaces.Common;
using Microsoft.AspNetCore.Http;

namespace MicroclimateIotSystem.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId => int.TryParse(
        _httpContextAccessor.HttpContext?.User.FindFirst(CustomClaimTypes.UserId)?.Value, 
        out var id) ? id : null;

    public string? Username => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst(CustomClaimTypes.Role)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
