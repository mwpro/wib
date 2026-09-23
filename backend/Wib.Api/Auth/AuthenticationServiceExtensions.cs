using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Wib.Api.Auth;

public class WibAuthenticationOptionsPostConfigure : IPostConfigureOptions<AuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public WibAuthenticationOptionsPostConfigure(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void PostConfigure(string? name, AuthenticationOptions options)
    {
        var bypassAuth = _configuration.GetValue<bool>("Testing:BypassAuth");
        if (bypassAuth)
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

        var auth0Domain = configuration["Auth0:Domain"];
        var audience = configuration["Auth0:Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (!string.IsNullOrWhiteSpace(auth0Domain))
            {
                options.Authority = $"https://{auth0Domain}/";
            }
            options.Audience = audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(auth0Domain),
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        })
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme,
            _ => { });

        services.AddSingleton<IPostConfigureOptions<AuthenticationOptions>, WibAuthenticationOptionsPostConfigure>();

        return services;
    }

    public static IApplicationBuilder UseJitMemberProvisioning(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JitMemberProvisioningMiddleware>();
    }
}
