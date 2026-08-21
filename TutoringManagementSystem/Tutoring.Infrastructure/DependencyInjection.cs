using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Infrastructure;


public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<TutoringDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        return services;
    }
}
