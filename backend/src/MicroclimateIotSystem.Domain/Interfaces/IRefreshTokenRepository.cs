using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Entities;

namespace MicroclimateIotSystem.Domain.Interfaces;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
}


