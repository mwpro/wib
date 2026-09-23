using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wib.Api.Auth;

namespace Wib.Api.Controllers;

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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;

    public MembersController(ICurrentMemberAccessor currentMemberAccessor)
    {
        _currentMemberAccessor = currentMemberAccessor;
    }

    [HttpGet("me")]
    public ActionResult<MemberDto> GetCurrentMember()
    {
        var member = _currentMemberAccessor.CurrentMember;
        if (member == null)
        {
            return Unauthorized();
        }

        return Ok(new MemberDto(
            member.Id,
            member.Auth0UserId,
            member.Name,
            member.Email,
            member.Picture,
            member.WalletBalance,
            member.EarnedPoints,
            member.CreatedAt
        ));
    }
}
