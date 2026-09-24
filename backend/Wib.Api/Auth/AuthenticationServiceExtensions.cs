using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wib.Api.Common;

namespace Wib.Api.Auth;

public class AuthenticationOptionsPostConfigure : IPostConfigureOptions<AuthenticationOptions>
{
    private readonly IOptions<JwtAuthOptions> _jwtAuthOptions;

    public AuthenticationOptionsPostConfigure(IOptions<JwtAuthOptions> jwtAuthOptions)
    {
        _jwtAuthOptions = jwtAuthOptions;
    }

    public void PostConfigure(string? name, AuthenticationOptions options)
    {
        if (_jwtAuthOptions.Value.BypassAuth)
        {
            options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
            options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
        }
    }
}

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddWibAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IJitMemberProvisioner, JitMemberProvisioner>();
        services.AddScoped<ICurrentMemberAccessor, CurrentMemberAccessor>();

        var jwtOptions = configuration.GetSection(JwtAuthOptions.SectionName).Get<JwtAuthOptions>() ?? new JwtAuthOptions();

        var authority = jwtOptions.GetNormalizedAuthority();
        var audience = jwtOptions.Audience;

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (!string.IsNullOrWhiteSpace(authority))
            {
                options.Authority = authority;
            }
            options.Audience = audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(authority),
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        });

        authBuilder.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme,
            _ => { });

        services.AddSingleton<IPostConfigureOptions<AuthenticationOptions>, AuthenticationOptionsPostConfigure>();

        return services;
    }
}
