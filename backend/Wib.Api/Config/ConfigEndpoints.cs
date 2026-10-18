using Microsoft.Extensions.Options;
using Sentry.AspNetCore;
using Wib.Api.Common;

namespace Wib.Api.Config;

public record JwtAuthConfigDto(string Authority, string Domain, string ClientId, string Audience);
public record SentryConfigDto(string? Dsn, string? Environment, string? Release);
public record ClientConfigResponse(JwtAuthConfigDto JwtAuth, bool IsTestMode, SentryConfigDto Sentry);

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", (IOptions<JwtAuthOptions> jwtAuthOptions, IOptions<SentryAspNetCoreOptions> sentryOptions) =>
        {
            var options = jwtAuthOptions.Value;

            var jwtDto = new JwtAuthConfigDto(
                options.Authority,
                options.GetDomain(),
                options.ClientId,
                options.Audience
            );

            var sentry = sentryOptions.Value;
            var sentryDto = new SentryConfigDto(sentry.Dsn, sentry.Environment, sentry.Release);

            return Results.Ok(new ClientConfigResponse(jwtDto, options.BypassAuth, sentryDto));
        })
        .AllowAnonymous()
        .WithName("GetConfig")
        .WithTags("Config");

        return app;
    }
}
