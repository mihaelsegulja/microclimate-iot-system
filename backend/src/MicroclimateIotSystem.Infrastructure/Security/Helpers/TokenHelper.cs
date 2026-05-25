using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Application.Constants;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace MicroclimateIotSystem.Infrastructure.Security.Helpers;

public class TokenHelper : ITokenHelper
{
    private readonly JwtConfig _jwtConfig;

    public TokenHelper(JwtConfig jwtConfig)
    {
        _jwtConfig = jwtConfig;
    }

    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            
            new Claim(CustomClaimTypes.UserId, user.Id.ToString()),
            new Claim(CustomClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

