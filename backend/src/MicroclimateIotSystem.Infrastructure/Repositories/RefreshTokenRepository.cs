using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Domain.Interfaces;
using MicroclimateIotSystem.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return _dbSet.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == token);
    }
}

