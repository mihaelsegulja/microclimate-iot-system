using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.Interfaces.Common;
using MicroclimateIotSystem.Application.Interfaces.Queue;
using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Interfaces;
using MicroclimateIotSystem.Infrastructure.Abstractions;
using MicroclimateIotSystem.Infrastructure.Messaging;
using MicroclimateIotSystem.Infrastructure.Repositories;
using MicroclimateIotSystem.Infrastructure.Security.Helpers;
using MicroclimateIotSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MicroclimateIotSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Db"), 
                x => 
                {
                    x.MigrationsAssembly("MicroclimateIotSystem.Infrastructure");
                    x.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                }));

        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<ITokenHelper, TokenHelper>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // TODO: Extract to a separate AddMessageBroker extension method
        services.AddSingleton<IConnection>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            // TODO: Handle connection failure gracefully with retry policy
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IMessageQueuePublisher, RabbitMqPublisher>();
        // TODO: Register more consumers as hosted services when needed
        services.AddHostedService<SensorDataConsumer>();

        return services;
    }
}
