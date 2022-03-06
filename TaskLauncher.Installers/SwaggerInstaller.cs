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
        services.InstallSwagger();
    }
}

public static class SwaggerExtenstions
{
    public static FluentSwagger AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer()
                .AddSwaggerGen();
        return new FluentSwagger(services);
    }

    public static IServiceCollection InstallSwagger(this IServiceCollection services)
    {
        return services
                .AddSwagger()
                .AddVersion("", "")
                .AddBearerSecurityScheme()
                .AddExamples()
                .Build();
    }
}

public class FluentSwagger
{
    private readonly SwaggerGenOptions options = new();
    private readonly IServiceCollection services;

    public FluentSwagger(IServiceCollection services)
    {
        this.services = services;
    }

    public FluentSwagger AddBearerSecurityScheme()
    {
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
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securitySchema);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securitySchema, new[] { JwtBearerDefaults.AuthenticationScheme } }
        });
        return this;
    }

    public FluentSwagger AddVersion(string version, string title)
    {
        options.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
        return this;
    }

    public FluentSwagger AddVersion(string version, OpenApiInfo apiInfo)
    {
        options.SwaggerDoc(version, apiInfo);
        return this;
    }

    public FluentSwagger AddExamples()
    {
        //options.ExampleFilters();
        //services.AddSwaggerExamplesFromAssemblyOf<CookieLessLoginRequestExample>();
        return this;
    }

    public IServiceCollection Build()
    {
        services.ConfigureSwaggerGen(opt =>
        {
            opt.SwaggerGeneratorOptions.SecurityRequirements = options.SwaggerGeneratorOptions.SecurityRequirements;
            opt.SwaggerGeneratorOptions.SecuritySchemes = options.SwaggerGeneratorOptions.SecuritySchemes;
            opt.SwaggerGeneratorOptions.SwaggerDocs = options.SwaggerGeneratorOptions.SwaggerDocs;
        });
        return services;
    }
}
