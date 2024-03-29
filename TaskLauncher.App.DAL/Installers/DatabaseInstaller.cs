﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Common.Installers;

namespace TaskLauncher.App.DAL.Installers;

/// <summary>
/// Do DI pridava db context a poskytovatele userid
/// </summary>
public class DatabaseInstaller : IInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserIdProvider, UserIdProvider>();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Default")));
    }
}
