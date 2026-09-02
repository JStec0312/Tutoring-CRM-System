using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Messaging.Outbox;
using Tutoring.Infrastructure.Messaging.RabbitMq;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddDbContext<TutoringDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();

        services.AddSingleton<IPublisher, RabbitMqPublisher>();

        return services;
    }

    public static IServiceCollection AddMessagingWorkers(
        this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<EmailConsumer>();

        return services;
    }
}