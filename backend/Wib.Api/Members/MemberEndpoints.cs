using Wib.Api.Auth;

namespace Wib.Api.Members;

public record MemberDto(
    int Id,
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

        group.MapGet("/me", (CurrentMember currentMember) =>
        {
            var member = currentMember.Member;

            return Results.Ok(new MemberDto(
                member.Id,
                member.Name,
                member.WalletBalance,
                member.CreatedAt
            ));
        })
        .WithName("GetCurrentMember");

        return app;
    }
}
