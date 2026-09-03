using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Api.Configuration;


public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT options (key and expiration) are configured in appsettings.json
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        if(jwtOptions == null)
        {
            throw new InvalidOperationException("JWT options are not configured properly.");
        }
        // Bearer Options
        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    jwtOptions.SecretKey)),

                        RoleClaimType = "role",
                        NameClaimType = "sub",

                        ClockSkew = TimeSpan.Zero
                    };
                
                // Refresh token options are configured in appsettings.json
            });
            services.Configure<RefreshTokenOptions>(
                configuration.GetSection(RefreshTokenOptions.SectionName));

        // Password policy options
        services.Configure<PasswordPolicyOptions>(
            configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.AddAuthorization();

        return services;
    }
}
