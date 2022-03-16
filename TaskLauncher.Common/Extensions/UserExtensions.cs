using Auth0.ManagementApi.Models;
using Mapster;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Common.Extensions;

public static class UserExtensions
{
    public static UserModel GetModel(this User user)
    {
        var config = new TypeAdapterConfig();
        config.ForType<User, UserModel>()
            .Ignore(i => i.UserMetadata)
            .Ignore(i => i.AppMetadata)
            .Ignore(i => i.ProviderAttributes);

        var model = user.Adapt<UserModel>(config);
        model.Vip = user.AppMetadata.vip.Value;
        model.Registered = user.AppMetadata.registered.Value;
        model.Original = user;

        var tmp = user.UserMetadata.picture;
        if(tmp is not null)
        {
            string? picture = user.UserMetadata.picture.Value;
            if (picture is not null)
                model.Picture = picture;
        }
        return model;
    }

    public static UserClaimsModel GetUserClaims(this Auth0.AuthenticationApi.Models.UserInfo userInfo)
    {
        var hasVipClaim = userInfo.AdditionalClaims.TryGetValue("https://wutshot-test-api.com/vip", out var vip);
        return new()
        {
            Blocked = false,
            TokenBalance = "100",
            Vip = hasVipClaim ? bool.Parse(vip!.ToString()) : false,
            EmailVerified = userInfo.EmailVerified!.Value
        };
    }
}
