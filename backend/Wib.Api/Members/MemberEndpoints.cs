using Wib.Api.Auth;

namespace Wib.Api.Members;

public record MemberDto(
    int Id,
    string ExternalSubjectId,
    string Name,
    int WalletBalance,
    DateTime CreatedAt
);

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members")
            .RequireAuthorization()
            .WithTags("Members");

        group.MapGet("/me", async (ICurrentMemberAccessor currentMemberAccessor, CancellationToken cancellationToken) =>
        {
            var member = await currentMemberAccessor.GetCurrentMemberAsync(cancellationToken);
            if (member == null)
            {
                throw new InvalidOperationException("Authenticated user has no provisioned member. JIT provisioning may have failed.");
            }

            return Results.Ok(new MemberDto(
                member.Id,
                member.ExternalSubjectId,
                member.Name,
                member.WalletBalance,
                member.CreatedAt
            ));
        })
        .WithName("GetCurrentMember");

        return app;
    }
}
