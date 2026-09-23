using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wib.Api.Common;

namespace Wib.Api.Controllers;

public record Auth0ConfigDto(string Domain, string ClientId, string Audience);
public record ClientConfigResponse(Auth0ConfigDto Auth0, bool IsTestMode);

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly Auth0Options _auth0Options;
    private readonly IConfiguration _configuration;

    public ConfigController(IOptions<Auth0Options> auth0Options, IConfiguration configuration)
    {
        _auth0Options = auth0Options.Value;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ClientConfigResponse> GetConfig()
    {
        var isTestMode = _configuration.GetValue<bool>("Testing:BypassAuth") ||
                         _configuration.GetValue<bool>("Testing:IsTestMode");

        return Ok(new ClientConfigResponse(
            new Auth0ConfigDto(
                _auth0Options.Domain,
                _auth0Options.ClientId,
                _auth0Options.Audience
            ),
            isTestMode
        ));
    }
}
