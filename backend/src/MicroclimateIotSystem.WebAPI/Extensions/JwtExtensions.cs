using System.Text;
using MicroclimateIotSystem.Application.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MicroclimateIotSystem.WebAPI.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JwtConfig");
        var jwtConf = jwtSection.Get<JwtConfig>();
        
        services.Configure<JwtConfig>(jwtSection);
        services.AddSingleton(jwtConf!);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConf!.Issuer,
                    ValidAudience = jwtConf.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConf.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
