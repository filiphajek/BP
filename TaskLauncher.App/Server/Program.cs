using Microsoft.AspNetCore.Authentication.Cookies;
using TaskLauncher.Common.Configuration;
using Auth0.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using MapsterMapper;
using Mapster;
using TaskLauncher.Common.Installers;
using TaskLauncher.Common.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.OData.Edm;
using TaskLauncher.Authorization;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Models;
using TaskLauncher.App.DAL.Installers;
using TaskLauncher.Authorization.Services;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.App.Server.Seeders;
using TaskLauncher.App.Server.Hub;
using TaskLauncher.App.Server.Extensions;
using TaskLauncher.App.Server.Filters;
using TaskLauncher.App.Server.Proxy;
using TaskLauncher.App.DAL;
using Microsoft.EntityFrameworkCore;
using MediatR;
using TaskLauncher.App.Server.Services;
using TaskLauncher.Common.Enums;
using System.Reflection;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Common;

var builder = WebApplication.CreateBuilder(args);

//edm model pro admin odata endpointy
static IEdmModel GetAdminEdmModel()
{
    ODataConventionModelBuilder builder = new();
    builder.EntitySet<PaymentResponse>("Payments");
    builder.EntitySet<TaskResponse>("Tasks");
    builder.EntitySet<BanResponse>("Bans");
    return builder.GetEdmModel();
}

//edm model pro uzivatelske odata endpointy
static IEdmModel GetUserEdmModel()
{
    ODataConventionModelBuilder builder = new();
    builder.EntitySet<PaymentResponse>("Payments");
    builder.EntitySet<TaskResponse>("Tasks");
    return builder.GetEdmModel();
}

//mediatr sluzba
builder.Services.AddMediatR(typeof(Program).Assembly);

//pridani kontroleru s error stranky a pridani protokolu odata
builder.Services.AddControllersWithViews(options =>
{
    options.AddJsonPatchInputFormatter();
    options.Filters.Add<AuthFilter>();
    options.Filters.Add<ValidationFilter>();
})
.AddOData(opt => opt
.AddRouteComponents("odata/user", GetUserEdmModel())
.AddRouteComponents("odata/admin", GetAdminEdmModel())
.Select().Expand().Filter().OrderBy().SetMaxTop(null).Count());

builder.Services.AddRazorPages();

//mapper
var config = new TypeAdapterConfig();
config.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();
builder.Services.InstallServicesInAssemblyOf<DatabaseInstaller>(builder.Configuration);

//auth config
builder.Services.Configure<Auth0Roles>(builder.Configuration.GetSection(nameof(Auth0Roles)));
builder.Services.Configure<Auth0ApiConfiguration>(builder.Configuration.GetSection(nameof(Auth0ApiConfiguration)));
var auth0config = new Auth0ApiConfiguration();
builder.Configuration.Bind(nameof(Auth0ApiConfiguration), auth0config);

//cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton(new CacheConfiguration<AccessToken> { AbsoluteExpiration = TimeSpan.FromHours(5) });
builder.Services.AddSingleton(new CacheConfiguration<UserClaimsModel> { AbsoluteExpiration = TimeSpan.FromSeconds(20) });
builder.Services.AddSingleton<Cache<AccessToken>>();
builder.Services.AddSingleton<Cache<UserClaimsModel>>();

builder.Services.AddSingleton<ManagementTokenService>();

//pristup do google bucket storage
builder.Services.Configure<StorageConfiguration>(builder.Configuration.GetSection(nameof(StorageConfiguration)));
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

//httpclient
builder.Services.AddHttpClient();

//auth0 cookie autentizace
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = $"https://{builder.Configuration["Auth0ApiConfiguration:Domain"]}/";
    options.Audience = builder.Configuration["Auth0ApiConfiguration:Audience"];
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-BlazorServer";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    options.Cookie.IsEssential = true;
})
.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = auth0config.Domain;
    options.ClientSecret = auth0config.ClientSecret;
    options.ClientId = auth0config.ClientId;
    options.Scope = "openid profile email";
    options.SkipCookieMiddleware = true;
    options.CallbackPath = "/signin-oidc";
})
.WithAccessToken(options =>
{
    options.Audience = auth0config.Audience;
    options.UseRefreshTokens = true;
});

builder.Services.AddScoped<ITaskService, TaskService>();

//autorizace
builder.Services.AddAuthorizationServer(builder.Configuration);

//access token management
builder.Services.AddAccessTokenManagement();

//pridani proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddProxyMiddlewares(builder.Configuration);
builder.Services.Configure<ReverseProxyHandlers>(builder.Configuration.GetSection("ReverseProxyExtensions"));

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

//pridani signalr s pomocnym in memory ulozistem vsech real-time spojeni
builder.Services.AddSingleton<SignalRMemoryStorage>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<TaskCache>();
builder.Services.AddSingleton<Balancer>();
builder.Services.Configure<PriorityQueuesConfiguration>(builder.Configuration.GetSection("PriorityQueues"));

//pridani open api dokumentace
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<SwaggerFilter>();
    options.DocumentFilter<JsonPatchDocumentFilter>();

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskLauncher", Version = "v1" });
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
    options.AddSecurityDefinition("Bearer", securitySchema);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, new[] { "Bearer" } }
    });

    options.OperationFilter<ODataOperationFilter>();

    var xml1 = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xml2 = $"{Assembly.GetAssembly(typeof(BanUserRequest))!.GetName().Name}.xml";
    var xml3 = $"{Assembly.GetAssembly(typeof(EventModel))!.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml1));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml2));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml3));
});

//pridani seedovacich trid
builder.Services.AddScoped<Seeder>();
builder.Services.AddScoped<Configurator>();

//auth0 klientské faktory
builder.Services.InstallClientFactories();
//registrace pomocne servise tridy pro ziskani aktualniho uzivatele
builder.Services.AddScoped<IAuth0UserProvider, Auth0UserProvider>();

//pridani health kontroly
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DefaultModelsExpandDepth(-1);
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskLauncherDocumentation");
});

//pouzivani vestavenych middlewaru, middlewary jsou volany presne v tomto poradi
app.UseHealthChecks("/health");

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
}).RequireAuthorization(Constants.Policies.AdminPolicy);

//namapovani koncovych bodu na kontrolery a signalr
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapHub<WorkerHub>("/WorkerHub");
    endpoints.MapHub<UserHub>("/UserHub");
});

var balancer = app.Services.GetRequiredService<Balancer>();

using (var scope = app.Services.CreateScope())
{
    //urceni zda se budou seedovat data
    var seeding = bool.Parse(builder.Configuration["SeederConfig:seed"]);

    if (seeding)
    {
        //seedovani
        var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
        await seeder.SeedAsync();
    }
    else
    {
        //ujisteni zda databaze existuje
        var ensureContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ensureContext.Database.EnsureCreatedAsync();
    }

    //inicializace kongiguracnich promennych pokud jiz nejsou v databazi
    var configurator = scope.ServiceProvider.GetRequiredService<Configurator>();
    await configurator.ConfigureDefaultsAsync();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var tasks = await dbContext.Tasks.IgnoreQueryFilters()
        .Where(i => i.ActualStatus == TaskState.Created || 
                    i.ActualStatus == TaskState.Crashed ||
                    i.ActualStatus == TaskState.Running ||
                    i.ActualStatus == TaskState.Ready)
        .ToListAsync();
    
    //inicializace fronty
    foreach(var task in tasks)
    {
        var queue = task.IsPriority ? "vip" : "nonvip";
        balancer.Enqueue(queue, new()
        {
            State = 0,
            Id = task.Id,
            Name = task.Name,
            IsPriority = task.IsPriority,
            TaskFilePath = task.TaskFile,
            ResultFilePath = task.ResultFile,
            Time = DateTime.Now,
            UserId = task.UserId
        });
    }
}

app.Run();
