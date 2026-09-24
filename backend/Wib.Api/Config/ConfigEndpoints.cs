using Microsoft.Extensions.Options;
using Wib.Api.Common;

namespace Wib.Api.Config;

public record JwtAuthConfigDto(string Authority, string Domain, string ClientId, string Audience);
public record ClientConfigResponse(JwtAuthConfigDto JwtAuth, bool IsTestMode);

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", (IOptions<JwtAuthOptions> jwtAuthOptions) =>
        {
            var options = jwtAuthOptions.Value;

            var jwtDto = new JwtAuthConfigDto(
                options.Authority,
                options.GetDomain(),
                options.ClientId,
                options.Audience
            );

            return Results.Ok(new ClientConfigResponse(jwtDto, options.BypassAuth));
        })
        .AllowAnonymous()
        .WithName("GetConfig")
        .WithTags("Config");

        return app;
    }
}
