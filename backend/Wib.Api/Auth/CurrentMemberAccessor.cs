using Wib.Api.Data.Entities;

namespace Wib.Api.Auth;

public interface ICurrentMemberAccessor
{
    Member? CurrentMember { get; }
}

public class CurrentMemberAccessor : ICurrentMemberAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentMemberAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Member? CurrentMember => _httpContextAccessor.HttpContext?.Items["CurrentMember"] as Member;
}
