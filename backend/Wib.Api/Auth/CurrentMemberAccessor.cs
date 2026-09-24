using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Wib.Api.Data.Entities;

namespace Wib.Api.Auth;

public interface ICurrentMemberAccessor
{
    public const string HttpContextItemKey = "CurrentMember";

    Task<Member?> GetCurrentMemberAsync(CancellationToken cancellationToken = default);
    Member? CurrentMember { get; }
}

public class CurrentMemberAccessor : ICurrentMemberAccessor, IDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJitMemberProvisioner _provisioner;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Member? _currentMember;
    private bool _disposed;

    public CurrentMemberAccessor(
        IHttpContextAccessor httpContextAccessor,
        IJitMemberProvisioner provisioner)
    {
        _httpContextAccessor = httpContextAccessor;
        _provisioner = provisioner;
    }

    public Member? CurrentMember =>
        _currentMember ?? _httpContextAccessor.HttpContext?.Items[ICurrentMemberAccessor.HttpContextItemKey] as Member;

    public async Task<Member?> GetCurrentMemberAsync(CancellationToken cancellationToken = default)
    {
        if (_currentMember != null)
        {
            return _currentMember;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_currentMember != null)
            {
                return _currentMember;
            }

            var context = _httpContextAccessor.HttpContext;
            if (context == null || context.User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            if (context.Items.TryGetValue(ICurrentMemberAccessor.HttpContextItemKey, out var item) && item is Member member)
            {
                _currentMember = member;
                return _currentMember;
            }

            _currentMember = await _provisioner.ProvisionMemberAsync(context.User, cancellationToken);
            if (_currentMember != null)
            {
                context.Items[ICurrentMemberAccessor.HttpContextItemKey] = _currentMember;
            }

            return _currentMember;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }
}
