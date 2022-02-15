using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TaskLauncher.Auth;

public class SeedData
{
    public static void EnsureSeedData(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AspNetIdentityDbContext>(
            options => options.UseSqlServer(connectionString)
        );

        services
            .AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<AspNetIdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddOperationalDbContext(
            options =>
            {
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName)
                    );
            }
        );
        services.AddConfigurationDbContext(
            options =>
            {
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(
                        connectionString,
                        sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName)
                    );
            }
        );

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        //delete
        var pers = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        pers.Database.EnsureDeleted();

        var conf = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        conf.Database.EnsureDeleted();

        var ident = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
        ident.Database.EnsureDeleted();

        //create
        scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.Database.Migrate();

        EnsureSeedData(context);

        var ctx = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
        ctx.Database.Migrate();
        EnsureUsers(scope);
    }

    private static void EnsureUsers(IServiceScope scope)
    {
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var angella = userMgr.FindByNameAsync("angella").Result;
        if (angella == null)
        {
            angella = new IdentityUser
            {
                UserName = "angella",
                Email = "angella.freeman@email.com",
                EmailConfirmed = true
            };
            var result = userMgr.CreateAsync(angella, "Pass123$").Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result =
                userMgr.AddClaimsAsync(
                    angella,
                    new Claim[]
                    {
                            new Claim(JwtClaimTypes.Name, "Filip Hajek"),
                            new Claim(JwtClaimTypes.GivenName, "Filip"),
                            new Claim(JwtClaimTypes.FamilyName, "Hajek"),
                            new Claim("location", "Praha")
                    }
                ).Result;
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
        }
    }

    private static void EnsureSeedData(ConfigurationDbContext context)
    {
        if (!context.Clients.Any())
        {
            foreach (var client in Config.Clients.ToList())
            {
                context.Clients.Add(client.ToEntity());
            }

            context.SaveChanges();
        }

        if (!context.IdentityResources.Any())
        {
            foreach (var resource in Config.IdentityResources.ToList())
            {
                context.IdentityResources.Add(resource.ToEntity());
            }

            context.SaveChanges();
        }

        if (!context.ApiScopes.Any())
        {
            foreach (var resource in Config.ApiScopes.ToList())
            {
                context.ApiScopes.Add(resource.ToEntity());
            }

            context.SaveChanges();
        }

        if (!context.ApiResources.Any())
        {
            foreach (var resource in Config.ApiResources.ToList())
            {
                context.ApiResources.Add(resource.ToEntity());
            }

            context.SaveChanges();
        }
    }
}