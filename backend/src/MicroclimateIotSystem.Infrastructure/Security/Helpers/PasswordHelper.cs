using MicroclimateIotSystem.Application.Common.Interfaces.Security;

namespace MicroclimateIotSystem.Infrastructure.Security.Helpers;

public class PasswordHelper : IPasswordHelper
{
    public string GenerateSalt()
    {
        return BCrypt.Net.BCrypt.GenerateSalt();
    }

    public string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, salt);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}


