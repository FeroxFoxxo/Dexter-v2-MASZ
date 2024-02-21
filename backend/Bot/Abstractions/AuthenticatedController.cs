using Bot.Services;
using Microsoft.AspNetCore.Authorization;

namespace Bot.Abstractions;

[Authorize]
public abstract class AuthenticatedController(IdentityManager identityManager, params Repository[] repositories) : BaseController
{
    private readonly IdentityManager _identityManager = identityManager;
    private readonly Repository[] _repositories = repositories;

    protected async Task<Identity> SetupAuthentication()
    {
        var identity = await _identityManager.GetIdentity(HttpContext);

        foreach (var repo in _repositories)
            repo.AsUser(identity);

        return identity;
    }
}
