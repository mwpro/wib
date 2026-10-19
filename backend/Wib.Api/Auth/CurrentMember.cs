using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Data;
using Wib.Domain.Members;

namespace Wib.Api.Auth;

public sealed class CurrentMember
{
    private CurrentMember(Member member)
    {
        Member = member;
    }

    public Member Member { get; }

    public static async ValueTask<CurrentMember> BindAsync(HttpContext context)
    {
        var (sub, name) = ReadIdentity(context.User);

        var services = context.RequestServices;
        var db = services.GetRequiredService<WibDbContext>();
        var nowUtc = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var logger = services.GetRequiredService<ILogger<CurrentMember>>();

        var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == sub, context.RequestAborted);
        if (member == null)
        {
            member = await ProvisionAsync(db, sub, name, nowUtc, logger, context.RequestAborted);
        }
        else
        {
            member.UpdateProfile(name, nowUtc);
            await db.SaveChangesAsync(context.RequestAborted);
        }

        return new CurrentMember(member);
    }

    private static (string Sub, string Name) ReadIdentity(ClaimsPrincipal principal)
    {
        var sub = principal.Identity?.IsAuthenticated == true ? principal.GetSubject() : null;
        if (string.IsNullOrWhiteSpace(sub))
        {
            throw new InvalidOperationException("CurrentMember requires an authenticated request with a subject claim.");
        }

        var name = principal.FindFirst("name")?.Value
                   ?? principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? principal.FindFirst("nickname")?.Value
                   ?? sub;

        return (sub, name);
    }

    private static async Task<Member> ProvisionAsync(
        WibDbContext db, string sub, string name, DateTime nowUtc, ILogger logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("JIT provisioning new member for sub: {Sub}", sub);
        var member = Member.Create(sub, name, nowUtc);
        db.Members.Add(member);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return member;
        }
        catch (DbUpdateException)
        {
            logger.LogInformation("JIT provisioning conflict for sub: {Sub}, re-fetching existing member", sub);
            db.Entry(member).State = EntityState.Detached;
            return await db.Members.FirstAsync(m => m.ExternalSubjectId == sub, cancellationToken);
        }
    }
}
