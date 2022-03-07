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
        model.Original = user;
        return model;
    }
}