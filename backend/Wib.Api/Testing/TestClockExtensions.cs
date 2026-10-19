namespace Wib.Api.Testing;

public record SetTestClockRequest(DateTimeOffset UtcNow);

public static class TestClockExtensions
{
    public static IServiceCollection AddWibClock(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddSingleton<TestClock>();
            services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<TestClock>());
        }
        else
        {
            services.AddSingleton(TimeProvider.System);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapTestClockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test/clock").WithTags("Testing");

        group.MapPut("/", (SetTestClockRequest request, TestClock clock) =>
        {
            clock.SetUtcNow(request.UtcNow);
            return Results.NoContent();
        })
        .WithName("SetTestClock");

        group.MapDelete("/", (TestClock clock) =>
        {
            clock.Reset();
            return Results.NoContent();
        })
        .WithName("ResetTestClock");

        return app;
    }
}
