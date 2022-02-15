using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ProxyKit;
using TaskLauncher.Common.Configuration;
using TaskLauncher.WebApp.Server.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using TaskLauncher.WebApp.Server;
//using Blazor.HttpProxy.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json");

//vycteni konfigurace z appsettings.json
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);

//pridani kontroleru s error stranky
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "__Host-BlazorServer";
    options.Cookie.SameSite = SameSiteMode.Lax;
})
//openid konfigurace, v budoucnu bude tato konfigurace zmenena, vyuzije se novy balicek od Auth0
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    //identityserver
    //options.Authority = "https://localhost:7034";
    //options.ClientId = "gateway";
    //options.ClientSecret = "secret";
    //options.ResponseType = OpenIdConnectResponseType.Code;
    /*options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = JwtClaimTypes.GivenName,
        RoleClaimType = JwtClaimTypes.Role
    };*/

    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
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
            var logoutUri = $"https://{builder.Configuration["Auth0:Domain"]}/v2/logout?client_id={builder.Configuration["Auth0:ClientId"]}";

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
            context.ProtocolMessage.SetParameter("audience", builder.Configuration["Auth0:Audience"]);
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

//pridani proxy z balicku ProxyKit
/*builder.Services.AddAccessTokenManagement();
builder.Services.AddProxy((clientBuilder) =>
{
    clientBuilder.AddUserAccessTokenHandler();
});*/

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

//pridani signalr s pomocnym in memory ulozistem vsech real-time spojeni
builder.Services.AddSingleton<SignalRMemoryStorage>();
builder.Services.AddSignalR();

builder.Services.AddOcelot();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//string testAccessToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImI1Wk1YcmFOOE82YUlxTUJtZnhDViJ9.eyJpc3MiOiJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vIiwic3ViIjoiYXV0aDB8NjE4YWM3NzcxMGFjYWUwMDZhZDA3ZjFiIiwiYXVkIjpbImh0dHBzOi8vd3V0c2hvdC10ZXN0LWFwaS5jb20iLCJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vdXNlcmluZm8iXSwiaWF0IjoxNjM4NDgzMzA0LCJleHAiOjE2Mzg1Njk3MDQsImF6cCI6Ijd3bjBsRG5COWhWNjJtODZ6aDhYYjM3NEtoSHhPaXJKIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCJ9.MuYAb1WSh7Gts9FrJ8cau13TE3V8ocWlDdRYiVumNbwrLG_asb3xLLEQr9W9KfDuVZbdNwi-9pIzW5aeBwS_RzutNnm533p2GSXi1OwRGeXu9Bylh3yHB_ltbYPpdQo7DMPwmk9ptQbc720BLWqixDGHSaYgIps4p8Ik9y7dnJpNpYg2U-H3WOwreUdBe9j-MO6i6sdyoXyqv7vUondb2bxHNieVBGeR7w244LSHs-Kr3MRRhtjGMUOttXUK4a7XytNLjMgGHwU_oZ8OXXwIJzC2Z-dr7uVC-LlRtwWqMie3zrTVb41JKzth_9gyB-9PTOukodzQQVI6qIlAFXliTA";
string testAccessToken = "";

//presmerovani http dotazu na TaskLauncher.Api
/*app.Map("/proxy", api =>
{
    api.RunProxy(async context =>
    {
        var config = app.Services.GetRequiredService<ServiceAddresses>();
        var forwardContext = context.ForwardTo(config.WebApiAddress);

        string accessToken = testAccessToken;
        if (string.IsNullOrEmpty(accessToken))
        {
            //z aktualniho cookie kontextu ziskej access token k TaskLauncher.Api
            accessToken = await context.GetTokenAsync("access_token");
        }
        //pridani autorizacniho tokenu
        forwardContext.HttpContext.Request.Headers.Add("Authorization", $"Bearer {accessToken}");

        return await forwardContext.Send();
    });
});*/

app.MapRazorPages();
app.MapControllers();

app.UseOcelotWhenRouteMatch();

app.MapFallbackToFile("index.html");

//pridani signalr endpointu
app.UseEndpoints(routes =>
{
    routes.MapHub<LauncherHub>("/LauncherHub");
});

app.Run();

