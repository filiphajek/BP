using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskLauncher.Common.Installers;

public class SwaggerInstaller : IInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }
}

public static class SwaggerInstallerExtenstions
{
    //TODO udelat z toho fluent api builder -> InstallSwagger().AddVersioning().AddSecurity()...
    public static void InstallSwagger(IServiceCollection services, SwaggerGenOptions? options = null)
    {
        services.AddEndpointsApiExplorer()
        .AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskLauncher", Version = "v1" });

            var securitySchema = new OpenApiSecurityScheme
            {
                Description = "Using the Authorization header with the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securitySchema);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securitySchema, new[] { JwtBearerDefaults.AuthenticationScheme } }
            });
        });
    }
}