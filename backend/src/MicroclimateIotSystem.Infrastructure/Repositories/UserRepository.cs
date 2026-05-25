using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Domain.Interfaces;
using MicroclimateIotSystem.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        return _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }
}
