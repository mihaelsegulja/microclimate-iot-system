using MicroclimateIotSystem.Domain.Entities;

namespace MicroclimateIotSystem.Application.Common.Interfaces.Security;

public interface ITokenHelper
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}


