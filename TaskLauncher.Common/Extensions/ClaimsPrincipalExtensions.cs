using System.Security.Claims;

namespace TaskLauncher.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAuth0Id(this ClaimsPrincipal principal, out string id)
    {
        id = string.Empty;
        //FirstOrDefault kvuli tomu, ze mohu poslat request ze swaggeru a jelikoz je to na stejne adrese a jsme prihlaseni, posle se i cookie -> 2x ClaimsPrincipal
        var subClaim = principal.Claims.FirstOrDefault(i => i.Type == ClaimTypes.NameIdentifier);
        if (subClaim is null)
            return false;

        id = subClaim.Value;
        return true;
    }

    public static bool TryGetClaimValue(this ClaimsPrincipal principal, string type, out string value)
    {
        value = string.Empty;
        var subClaim = principal.Claims.FirstOrDefault(i => i.Type == type);
        if (subClaim is null)
            return false;

        value = subClaim.Value;
        return true;
    }
}
