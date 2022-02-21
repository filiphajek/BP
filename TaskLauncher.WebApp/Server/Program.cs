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

//defaultni cookie autentizace
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
//pro signalR se lze prihlasit pres JWT bearer
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Authority = "https://dev-8nhuxay1.us.auth0.com/";
    options.Audience = "https://wutshot-test-api.com";
})
//cookie konfigurace
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-BlazorServer";
    options.Cookie.SameSite = SameSiteMode.Strict;
})
//openid konfigurace, v budoucnu bude tato konfigurace zmenena, vyuzije se novy balicek od Auth0
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = $"https://{auth0config.Domain}";
    options.ClientId = auth0config.ClientId;
    options.ClientSecret = auth0config.ClientSecret;
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.CallbackPath = new PathString("/signin-oidc");
    options.ClaimsIssuer = "Auth0";
    options.SaveTokens = true;
    options.UsePkce = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters.NameClaimType = "name";

    options.Events = new OpenIdConnectEvents
    {
        //logout presmetovani
        OnRedirectToIdentityProviderForSignOut = (context) =>
        {
            var logoutUri = $"https://{auth0config.Domain}/v2/logout?client_id={auth0config.ClientId}";

            var postLogoutUri = context.Properties.RedirectUri;
            if (!string.IsNullOrEmpty(postLogoutUri))
            {
                if (postLogoutUri.StartsWith("/"))
                {
                    var request = context.Request;
                    postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                }
                logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
            }

            context.Response.Redirect(logoutUri);
            context.HandleResponse();

            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = context =>
        {
            //pridani audience pro ziskani autorizacniho tokenu k web api
            context.ProtocolMessage.SetParameter("audience", auth0config.Audience);
            return Task.FromResult(0);
        }
    };
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
}).AllowAnonymous(); // pro testing jinak RequireAuthorization

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapHub<LauncherHub>("/LauncherHub");
});

app.Run();
