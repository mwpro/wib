using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wib.Api.Data;
using Wib.Api.Data.Entities;

namespace Wib.Api.Auth;

public interface IJitMemberProvisioner
{
    Task<Member?> ProvisionMemberAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public class JitMemberProvisioner : IJitMemberProvisioner
{
    private readonly WibDbContext _dbContext;
    private readonly ILogger<JitMemberProvisioner> _logger;

    public JitMemberProvisioner(WibDbContext dbContext, ILogger<JitMemberProvisioner> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Member?> ProvisionMemberAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = principal.FindFirst("sub")?.Value 
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return null;
        }

        var name = principal.FindFirst("name")?.Value 
                ?? principal.FindFirst(ClaimTypes.Name)?.Value 
                ?? principal.FindFirst("nickname")?.Value 
                ?? sub;

        var member = await _dbContext.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == sub, cancellationToken);

        if (member == null)
        {
            _logger.LogInformation("JIT provisioning new member for sub: {Sub}", sub);
            member = new Member
            {
                ExternalSubjectId = sub,
                Name = name,
                WalletBalance = 0,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Members.Add(member);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(name) && member.Name != name)
            {
                member.Name = name;
                changed = true;
            }

            if (changed)
            {
                _logger.LogInformation("JIT updating profile for member sub: {Sub}", sub);
                member.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return member;
    }
}
