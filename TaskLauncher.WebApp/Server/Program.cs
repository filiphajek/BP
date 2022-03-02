using Microsoft.AspNetCore.Authentication.Cookies;
using TaskLauncher.Common.Configuration;
using TaskLauncher.WebApp.Server.Hub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TaskLauncher.WebApp.Server.Services;
using TaskLauncher.WebApp.Server.Auth0;
using TaskLauncher.WebApp.Server.Proxy;
using TaskLauncher.WebApp.Server.Extensions;
using Auth0.AspNetCore.Authentication;
using TaskLauncher.Api.Controllers;
using Microsoft.OpenApi.Models;
using MapsterMapper;
using Mapster;
using TaskLauncher.Common.Installers;
using TaskLauncher.Api.DAL.Installers;
using TaskLauncher.Common.Services;
using RawRabbit.vNext;
using TaskLauncher.Common.RawRabbit;
using Swashbuckle.AspNetCore.Filters;
using TaskLauncher.Api.Contracts.SwaggerExamples;
using TaskLauncher.Api.Seeders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRawRabbit(cfg => cfg.AddJsonFile("rawrabbit.json"));
builder.Services.InstallRawRabbitExtensions();
builder.Services.Configure<RawRabbitConfiguration>(builder.Configuration.GetSection("RawRabbitExtensions"));

//vycteni konfigurace z appsettings.json
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);

//pridani kontroleru s error stranky
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(TasksController).Assembly);
builder.Services.AddRazorPages();

var config = new TypeAdapterConfig();
config.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();
builder.Services.InstallServicesInAssemblyOf<RepositoryInstaller>(builder.Configuration);

//auth config
builder.Services.Configure<Auth0ApiConfiguration>(builder.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
var auth0config = new Auth0ApiConfiguration();
builder.Configuration.Bind(nameof(Auth0ApiConfiguration), auth0config);

//cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<Cache<AccessToken>>();
builder.Services.AddScoped<ManagementTokenService>();

//pristup do google bucket storage
builder.Services.Configure<StorageConfiguration>(builder.Configuration.GetSection(nameof(StorageConfiguration)));
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

//httpclient
builder.Services.AddHttpClient();

//auth0 autentizace
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "https://dev-8nhuxay1.us.auth0.com/";
    options.Audience = "https://wutshot-test-api.com";
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-BlazorServer";
    options.Cookie.SameSite = SameSiteMode.Strict;
})
//TODO chybi oidc config
.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = auth0config.Domain;
    options.ClientSecret = auth0config.ClientSecret;
    options.ClientId = auth0config.ClientId;
    options.Scope = "openid profile email";
    options.SkipCookieMiddleware = true;
})
.WithAccessToken(options =>
{
    options.Audience = auth0config.Audience;
    options.UseRefreshTokens = true;
});

//autorizacni pravidlo pro signalr endpoint
builder.Services.AddAuthorization(policies =>
{
    policies.AddPolicy("admin-policy", p =>
    {
        p.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
        p.RequireRole("admin");
    });

    policies.AddPolicy("user-policy", p =>
    {
        p.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme);
        p.RequireRole("user");
    });

    policies.AddPolicy("launcher", p =>
    {
        p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        p.RequireClaim("azp", "1MBhNBPqfSs8FYlaHoFLe2uRwa5BV5Qa");
        p.RequireClaim("gty", "client-credentials");
    });
});

//access token management
builder.Services.AddAccessTokenManagement();

//pridani proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddProxyMiddlewares(builder.Configuration);
//TODO udelat nejaky check teto konfigurace .. aby routeId byla stejna
builder.Services.Configure<ReverseProxyHandlers>(builder.Configuration.GetSection("ReverseProxyExtensions"));

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

//pridani signalr s pomonym in memory ulozistem vsech real-time spojeni
builder.Services.AddSingleton<SignalRMemoryStorage>();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskLauncher", Version = "v1" });
    c.ExampleFilters();
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
builder.Services.AddSwaggerExamplesFromAssemblyOf<CookieLessLoginRequestExample>();

builder.Services.AddScoped<Seeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskLauncherDocumentation"));
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//presmerovani http dotazu
app.MapReverseProxy(opt =>
{
    opt.UseProxyMiddlewares<Program>();
}).RequireAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapHub<LauncherHub>("/LauncherHub");
});

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
    await seeder.SeedAsync();
}

app.Run();
