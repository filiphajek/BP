using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using TaskLauncher.Api.Authorization;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Installers;
using TaskLauncher.Api.Filters;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Installers;
using TaskLauncher.Common.Services;

var builder = WebApplication.CreateBuilder(args);

//konfigurace kontroleru
builder.Services
.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddFluentValidation(fluent =>
{
    fluent.RegisterValidatorsFromAssembly(typeof(Program).Assembly);
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

//mapster mapper
var config = new TypeAdapterConfig();
config.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

//swagger dokumentace
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securitySchema, new[] { "Bearer" } }
        });
});

//nastaveni cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(opt =>
    {
        opt
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddAuthorizationHandlers<Program>();

//autentiace/autorizace pres jwt bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://dev-8nhuxay1.us.auth0.com/";
    options.Audience = "https://wutshot-test-api.com";
});
builder.Services.AddAuthorization(policies =>
{
    policies.AddPolicy("updateToken", p =>
    {
        p.Requirements.Add(new AdminHandlerRequirement());
    });

    policies.AddPolicy("p-user-api-auth0", p =>
    {
        //p.Requirements.Add(new UserApiScopeHandlerRequirement());
        p.RequireClaim("azp", "7wn0lDnB9hV62m86zh8Xb374KhHxOirJ");
    });

    policies.AddPolicy("launcher", p =>
    {
        p.RequireClaim("azp", "1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa");
        p.RequireClaim("gty", "client-credentials");
    });
});

//pristup do databaze
builder.Services.InstallServicesInAssemblyOf<RepositoryInstaller>(builder.Configuration);

//pristup do google bucket storage
builder.Services.AddSingleton(services =>
{
    var config = services.GetRequiredService<IConfiguration>();
    var tmp = new StorageConfiguration();
    config.Bind(nameof(StorageConfiguration), tmp);
    return tmp;
});
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FirebaseDemo v1"));
}

app.UseCors();
//app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//vytvoreni databaze
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
