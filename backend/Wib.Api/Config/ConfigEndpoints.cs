using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wib.Api.Common;

namespace Wib.Api.Config;

public record JwtAuthConfigDto(string Authority, string Domain, string ClientId, string Audience);
public record ClientConfigResponse(JwtAuthConfigDto JwtAuth, bool IsTestMode)
{
    public JwtAuthConfigDto Auth0 => JwtAuth;
}

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", (
            IOptions<JwtAuthOptions> jwtAuthOptions,
            IConfiguration configuration) =>
        {
            var isTestMode = configuration.GetValue<bool>("Testing:BypassAuth") ||
                             configuration.GetValue<bool>("Testing:IsTestMode");

            var options = jwtAuthOptions.Value;

            var jwtDto = new JwtAuthConfigDto(
                options.Authority,
                options.GetDomain(),
                options.ClientId,
                options.Audience
            );

            return Results.Ok(new ClientConfigResponse(jwtDto, isTestMode));
        })
        .AllowAnonymous()
        .WithName("GetConfig")
        .WithTags("Config");

        return app;
    }
}
