using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TaskLauncher.Common.Configuration;
using TaskLauncher.WebApp.Server.Hub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Yarp.ReverseProxy.Configuration;
using TaskLauncher.WebApp.Server.Services;
using TaskLauncher.WebApp.Server.Auth0;
using TaskLauncher.WebApp.Server.Proxy;
using TaskLauncher.WebApp.Server.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Auth0.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

//vycteni konfigurace z appsettings.json
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);

//pridani kontroleru s error stranky
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

//auth config
builder.Services.Configure<Auth0ApiConfiguration>(builder.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
var auth0config = new Auth0ApiConfiguration();
builder.Configuration.Bind(nameof(Auth0ApiConfiguration), auth0config);

//cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<Cache<AccessToken>>();
builder.Services.AddScoped<ManagementTokenService>();

//httpclient
builder.Services.AddHttpClient();

//auth0 autentizace
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
}).WithAccessToken(options =>
{
    options.Audience = auth0config.Audience;
    options.UseRefreshTokens = true;
});

//autorizacni pravidlo pro signalr endpoint
builder.Services.AddAuthorization(policies =>
{
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

//pridani signalr s pomocnym in memory ulozistem vsech real-time spojeni
builder.Services.AddSingleton<SignalRMemoryStorage>();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
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

app.Run();
