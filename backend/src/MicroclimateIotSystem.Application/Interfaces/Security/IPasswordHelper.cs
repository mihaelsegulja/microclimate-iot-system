namespace MicroclimateIotSystem.Application.Common.Interfaces.Security;

public interface IPasswordHelper
{
    string HashPassword(string password, string salt);
    string GenerateSalt();
    bool VerifyPassword(string password, string hash);
}


