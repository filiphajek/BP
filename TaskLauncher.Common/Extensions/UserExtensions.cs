﻿using Auth0.ManagementApi.Models;
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
        return model;
    }

    public static UserClaimsModel GetUserClaims(this Auth0.AuthenticationApi.Models.UserInfo userInfo)
    {
        var hasTokenClaim = userInfo.AdditionalClaims.TryGetValue("token_balance", out var value);
        var hasVipClaim = userInfo.AdditionalClaims.TryGetValue("vip", out var vip);
        return new()
        {
            Blocked = false,
            TokenBalance = hasTokenClaim ? value!.ToString() : "",
            Vip = hasVipClaim ? bool.Parse(vip!.ToString()) : false,
            EmailVerified = userInfo.EmailVerified!.Value
        };
    }
}
