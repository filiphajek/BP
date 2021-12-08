using System.Security.Claims;

namespace TaskLauncher.Common.Extensions;

public static class Auth0Extenstions
{
    public static bool TryGetAuth0Id(this ClaimsPrincipal principal, out string id)
    {
        id = string.Empty;
        var subClaim = principal.Claims.SingleOrDefault(i => i.Type == ClaimTypes.NameIdentifier);
        if (subClaim is null)
            return false;

        var split = subClaim.Value.Split("|");
        if(split.Length != 2)
            return false;
        
        id = split.Last();
        return true;
    }
}
