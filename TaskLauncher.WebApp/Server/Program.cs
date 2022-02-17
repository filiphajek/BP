using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TaskLauncher.Common.Configuration;
using TaskLauncher.WebApp.Server.Hub;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

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
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-BlazorServer";
    options.Cookie.SameSite = SameSiteMode.Strict;
})
//openid konfigurace, v budoucnu bude tato konfigurace zmenena, vyuzije se novy balicek od Auth0
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
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

//access token managment
builder.Services.AddAccessTokenManagement();

//pridani proxy
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
    opt.Use(async (context, next) =>
    {
        var cache = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImI1Wk1YcmFOOE82YUlxTUJtZnhDViJ9.eyJpc3MiOiJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vIiwic3ViIjoiYXV0aDB8NjFiMGUxNjE2NzhhMGMwMDY4OTY0NGUwIiwiYXVkIjpbImh0dHBzOi8vd3V0c2hvdC10ZXN0LWFwaS5jb20iLCJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vdXNlcmluZm8iXSwiaWF0IjoxNjQ1MTEzMjgzLCJleHAiOjE2NDUxOTk2ODMsImF6cCI6Ijd3bjBsRG5COWhWNjJtODZ6aDhYYjM3NEtoSHhPaXJKIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCJ9.j0zD-yhn7Ol1WZRhzZuCOeMFfZswTUNX60S6iZBefurJfgtfz5ux_LEN2bdiictsESlu-EwXl-JykjLa3CZgA_m5Z3wYQxuTBYqB5i8cfyvE2eYM77iLLZ8jgImknAaw2tuB30Rb5iX2vEDbgKP4vnhUZtwb6zFvVOHFMccJFGSnogVuOb31fydUtc0VCCca3NmrYOiKDZSGWiDz-nvLUMpPC39yRNdyCDSYoxrkIwgHSph8DmughJTHohHInexpixtUr1BfBp2RK-dYFD4STm1VuBk8mlH3NaQVAqFBMOxylUJ2ByHKGSze0gXZBSluZS-Z57a5kspEInVRL7jkkw";

        if(string.IsNullOrEmpty(cache))
            cache = await context.GetTokenAsync("access_token");

        context.Request.Headers.Add("Authorization", $"Bearer {cache}");
        await next();

    });
}).AllowAnonymous();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapHub<LauncherHub>("/LauncherHub");
});

app.Run();
