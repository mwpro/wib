using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wib.Api.Common;

namespace Wib.Api.Controllers;

public record JwtAuthConfigDto(string Authority, string Domain, string ClientId, string Audience);
public record ClientConfigResponse(JwtAuthConfigDto JwtAuth, bool IsTestMode)
{
    public JwtAuthConfigDto Auth0 => JwtAuth;
}

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions;
    private readonly IConfiguration _configuration;

    public ConfigController(IOptions<JwtAuthOptions> jwtAuthOptions, IConfiguration configuration)
    {
        _jwtAuthOptions = jwtAuthOptions.Value;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ClientConfigResponse> GetConfig()
    {
        var isTestMode = _configuration.GetValue<bool>("Testing:BypassAuth") ||
                         _configuration.GetValue<bool>("Testing:IsTestMode");

        var effectiveAuthority = _jwtAuthOptions.GetEffectiveAuthority();
        var effectiveDomain = _jwtAuthOptions.GetEffectiveDomain();

        var jwtDto = new JwtAuthConfigDto(
            effectiveAuthority,
            effectiveDomain,
            _jwtAuthOptions.ClientId,
            _jwtAuthOptions.Audience
        );

        return Ok(new ClientConfigResponse(jwtDto, isTestMode));
    }
}
