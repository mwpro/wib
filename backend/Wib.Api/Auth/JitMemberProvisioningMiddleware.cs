using Microsoft.AspNetCore.Http;

namespace Wib.Api.Auth;

public class JitMemberProvisioningMiddleware
{
    private readonly RequestDelegate _next;

    public JitMemberProvisioningMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJitMemberProvisioner provisioner)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var member = await provisioner.ProvisionMemberAsync(context.User, context.RequestAborted);
            if (member != null)
            {
                context.Items["CurrentMember"] = member;
            }
        }

        await _next(context);
    }
}
