using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wib.Api.Auth;

namespace Wib.Api.Members;

public record MemberDto(
    int Id,
    string Auth0UserId,
    string Name,
    string? Email,
    string? Picture,
    int WalletBalance,
    int EarnedPoints,
    DateTime CreatedAt
);

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members")
            .RequireAuthorization()
            .WithTags("Members");

        group.MapGet("/me", (ICurrentMemberAccessor currentMemberAccessor) =>
        {
            var member = currentMemberAccessor.CurrentMember;
            if (member == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new MemberDto(
                member.Id,
                member.Auth0UserId,
                member.Name,
                member.Email,
                member.Picture,
                member.WalletBalance,
                member.EarnedPoints,
                member.CreatedAt
            ));
        })
        .WithName("GetCurrentMember");

        return app;
    }
}
