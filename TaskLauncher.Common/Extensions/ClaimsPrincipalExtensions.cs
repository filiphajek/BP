using System.Security.Claims;

namespace TaskLauncher.Common.Extensions;

/// <summary>
/// Extenze pro pracovani s ClaimsPrincipal
/// </summary>
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

    public static bool TryGetClaimAsBool(this ClaimsPrincipal principal, string type, out bool value)
    {
        value = false;
        var subClaim = principal.Claims.FirstOrDefault(i => i.Type == type);
        if (subClaim is null)
            return false;

        return bool.TryParse(subClaim.Value, out value);
    }

    public static bool TryGetClaimAsInt(this ClaimsPrincipal principal, string type, out int value)
    {
        value = default;
        var subClaim = principal.Claims.FirstOrDefault(i => i.Type == type);
        if (subClaim is null)
            return false;

        return int.TryParse(subClaim.Value, out value);
    }
}
