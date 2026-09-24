using Microsoft.AspNetCore.Http;
using Wib.Api.Data.Entities;

namespace Wib.Api.Auth;

public interface ICurrentMemberAccessor
{
    public const string HttpContextItemKey = "CurrentMember";

    Member? CurrentMember { get; }
}

public class CurrentMemberAccessor : ICurrentMemberAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentMemberAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Member? CurrentMember => _httpContextAccessor.HttpContext?.Items[ICurrentMemberAccessor.HttpContextItemKey] as Member;
}
