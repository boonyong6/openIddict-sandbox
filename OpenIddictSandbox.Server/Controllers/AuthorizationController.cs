using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddictSandbox.Server.Controllers;

// Note: Implementing a custom authorization controller is required to allow 
// OpenIddict to create tokens based on the identities and claims you provide.

// Example: Client Credentials grant.
public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public AuthorizationController(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        OpenIddictRequest? request = HttpContext.GetOpenIddictServerRequest();
        if (request is null) throw new InvalidOperationException();

        if (request.IsClientCredentialsGrantType())
        {
            // Note: The client credentials are automatically validated by OpenIddict:
            // if client_id or client_secret are invalid, this action won't be invoked.

            object application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty) ??
                throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity containing the claims that 
            // will be used to create an id_token, a token or a code.
            ClaimsIdentity identity = new(
                TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            // Use the client_id as the subject identifier.
            identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
            identity.SetClaim(Claims.Name, await  _applicationManager.GetDisplayNameAsync(application));

            identity.SetDestinations(static claim => claim.Type switch
            {
                // Allow the "name" claim to be stored in both the access and identity tokens
                // when the "profile" scope was granted (by calling principal.SetScope(...)).
                Claims.Name when claim.Subject!.HasScope(Scopes.Profile)
                    => [Destinations.AccessToken, Destinations.IdentityToken],
                // Otherwise, only store the claim in the access token.
                _ => [Destinations.AccessToken]
            });

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant is not implemented.");
    }

    public IActionResult Index()
    {
        return View();
    }
}
