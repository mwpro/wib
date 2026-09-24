using Wib.Api.Data.Entities;

namespace Wib.Api.Auth;

public interface ICurrentMemberAccessor
{
    Task<Member?> GetCurrentMemberAsync(CancellationToken cancellationToken = default);
}

public class CurrentMemberAccessor : ICurrentMemberAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJitMemberProvisioner _provisioner;
    private Member? _currentMember;

    public CurrentMemberAccessor(
        IHttpContextAccessor httpContextAccessor,
        IJitMemberProvisioner provisioner)
    {
        _httpContextAccessor = httpContextAccessor;
        _provisioner = provisioner;
    }

    public async Task<Member?> GetCurrentMemberAsync(CancellationToken cancellationToken = default)
    {
        if (_currentMember != null)
        {
            return _currentMember;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        _currentMember = await _provisioner.ProvisionMemberAsync(context.User, cancellationToken);
        return _currentMember;
    }
}
