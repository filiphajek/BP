using System.Security.Claims;

namespace TaskLauncher.Common.Extensions;

public static class Auth0Extenstions
{
    public static bool TryGetAuth0Id(this ClaimsPrincipal principal, out string id)
    {
        id = string.Empty;
        //FirstOrDefault kvuli tomu, ze mohu poslat request ze swaggeru a jelikoz je to na stejne adrese a jsme prihlaseni, posle se i cookie -> 2x ClaimsPrincipal
        var subClaim = principal.Claims.FirstOrDefault(i => i.Type == ClaimTypes.NameIdentifier);
        if (subClaim is null)
            return false;

        var split = subClaim.Value.Split("|");
        if(split.Length != 2)
            return false;
        
        id = split.Last();
        return true;
    }
}
