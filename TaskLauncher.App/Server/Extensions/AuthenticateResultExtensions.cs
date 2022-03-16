using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace TaskLauncher.App.Server.Extensions;

public static class AuthenticateResultExtensions
{
    /// <summary>
    /// Ulozi do identity claimy, ty ktere se nove zmenily se atualizuji
    /// </summary>
    public static bool UpdateChangedClaims(this AuthenticateResult auth, IEnumerable<Claim> refreshedClaims)
    {
        bool changed = false;
        var claimsIdentity = auth.Principal!.Identity as ClaimsIdentity;

        if (claimsIdentity is null)
            throw new ArgumentNullException(nameof(auth));

        List<Claim> claimsToDelete = new();
        List<Claim> claimsToAdd = new();
        foreach (var oldClaim in claimsIdentity.Claims)
        {
            foreach (var newClaim in refreshedClaims)
            {
                if (oldClaim.Type == newClaim.Type)
                {
                    if (oldClaim.Value != newClaim.Value)
                    {
                        changed = true;
                        claimsToDelete.Add(oldClaim);
                        claimsToAdd.Add(newClaim);
                    }
                }
            }
        }

        foreach (var claim in claimsToDelete)
        {
            claimsIdentity.RemoveClaim(claim);
        }
        claimsIdentity.AddClaims(claimsToAdd);

        return changed;
    }

    public static void AddOrUpdateClaim(this AuthenticateResult result, Claim claim)
    {
        var claimsIdentity = (result.Principal!.Identity as ClaimsIdentity)!;
        var tmp = claimsIdentity.Claims.SingleOrDefault(i => i.Type == claim.Type);
        if (tmp is not null)
            claimsIdentity.RemoveClaim(tmp);
        claimsIdentity.AddClaim(claim);
    }
}
