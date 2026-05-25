using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Entities;

namespace MicroclimateIotSystem.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}
