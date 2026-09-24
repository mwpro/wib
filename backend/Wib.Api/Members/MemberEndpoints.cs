using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        group.MapGet("/me", (ICurrentMemberAccessor currentMemberAccessor) =>
        {
            var member = currentMemberAccessor.CurrentMember;
            if (member == null)
            {
                return Results.Unauthorized();
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
