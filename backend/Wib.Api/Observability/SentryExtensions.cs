using System.Security.Claims;

namespace Wib.Api.Observability;

public static class SentryExtensions
{
    public static WebApplicationBuilder AddWibSentry(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseSentry(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Release))
            {
                options.Release = null;
            }
        });

        return builder;
    }

    public static IApplicationBuilder UseSentryUser(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var sub = context.User.FindFirst("sub")?.Value
                      ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(sub))
            {
                SentrySdk.ConfigureScope(scope => scope.User = new SentryUser { Id = sub });
            }

            await next();
        });
    }
}
