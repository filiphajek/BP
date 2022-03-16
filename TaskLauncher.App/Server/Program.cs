using Microsoft.AspNetCore.Authentication.Cookies;
using TaskLauncher.Common.Configuration;
using Auth0.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using MapsterMapper;
using Mapster;
using TaskLauncher.Common.Installers;
using TaskLauncher.Common.Services;
using Swashbuckle.AspNetCore.Filters;
using TaskLauncher.Api.Contracts.SwaggerExamples;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.OData.Edm;
using Hangfire;
using Hangfire.SqlServer;
using TaskLauncher.Authorization;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Models;
using TaskLauncher.App.DAL.Installers;
using TaskLauncher.Authorization.Services;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.App.Server.Seeders;
using TaskLauncher.App.Server.Hub;
using TaskLauncher.App.Server.Routines;
using TaskLauncher.App.Server.Extensions;
using TaskLauncher.App.Server.Filters;
using TaskLauncher.App.Server.Proxy;
using TaskLauncher.App.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

static IEdmModel GetAdminEdmModel()
{
    ODataConventionModelBuilder builder = new();
    builder.EntitySet<PaymentResponse>("Payment");
    builder.EntitySet<TaskResponse>("Task");
    builder.EntitySet<BanResponse>("Ban");
    return builder.GetEdmModel();
}

static IEdmModel GetUserEdmModel()
{
    ODataConventionModelBuilder builder = new();
    builder.EntitySet<PaymentResponse>("Payment");
    builder.EntitySet<TaskResponse>("Task");
    return builder.GetEdmModel();
}

//vycteni konfigurace z appsettings.json
var serviceAddresses = new ServiceAddresses();
builder.Configuration.Bind(nameof(ServiceAddresses), serviceAddresses);
builder.Services.AddSingleton(serviceAddresses);

//pridani kontroleru s error stranky a pridani protokolu odata
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<BanFilter>();
    options.Filters.Add<ValidationFilter>();
})
.AddOData(opt => opt
.AddRouteComponents("odata/user", GetUserEdmModel())
.AddRouteComponents("odata/admin", GetAdminEdmModel())
.Select().Expand().Filter().OrderBy().SetMaxTop(null).Count());

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
builder.Services.AddSingleton(new CacheConfiguration<AccessToken> { AbsoluteExpiration = TimeSpan.FromHours(5) });
builder.Services.AddSingleton(new CacheConfiguration<UserClaimsModel> { AbsoluteExpiration = TimeSpan.FromSeconds(20) });
builder.Services.AddSingleton<Cache<AccessToken>>();
builder.Services.AddSingleton<Cache<UserClaimsModel>>();

builder.Services.AddSingleton<ManagementTokenService>();
builder.Services.AddSingleton<ManagementApiClientFactory>();

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

//hangfire
builder.Services.AddRoutines<Program>();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

//autorizace
builder.Services.AddAuthorizationServer();

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
builder.Services.AddSingleton<TaskCache>();
builder.Services.AddSingleton<Balancer>();
builder.Services.Configure<PriorityQueuesConfiguration>(builder.Configuration.GetSection("PriorityQueues"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    //c.DocumentFilter<SwaggerFilter>();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskLauncher", Version = "v1" });
    //c.ExampleFilters();
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
//builder.Services.AddSwaggerExamplesFromAssemblyOf<CookieLessLoginRequestExample>();

builder.Services.AddScoped<Seeder>();
builder.Services.AddScoped<Configurator>();

builder.Services.AddHttpClient();
builder.Services.InstallClientFactories();
builder.Services.AddScoped<IAuth0UserProvider, Auth0UserProvider>();

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
}).RequireAuthorization("admin-policy");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapHub<WorkerHub>("/WorkerHub");
    endpoints.MapHub<UserHub>("/UserHub");
});

app.UseHangfireDashboard("/hangfire");

var balancer = app.Services.GetRequiredService<Balancer>();
//backend testing
/*for (int i = 0; i < 20; i++)
{
    balancer.Enqueue("nonvip", new() { Id = Guid.NewGuid(), State = TaskLauncher.Common.Enums.TaskState.Created, Time = DateTime.Now, TaskFilePath = $"NON-vip {i}" });
}

for (int i = 0; i < 20; i++)
{
    balancer.Enqueue("vip", new() { UserId = "auth0|61b0e161678a0c00689644e0", Id = Guid.NewGuid(), State = TaskLauncher.Common.Enums.TaskState.Created, Time = DateTime.Now, TaskFilePath = $"vip {i}" });
}*/

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
    await seeder.SeedAsync();

    var jobClient = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var routine = scope.ServiceProvider.GetRequiredService<FileDeletionRoutine>();
    jobClient.RemoveIfExists(nameof(FileDeletionRoutine));
    //jobClient.AddOrUpdate(nameof(FileDeletionRoutine), () => routine.Perform(), Cron.Minutely);

    var configurator = scope.ServiceProvider.GetRequiredService<Configurator>();
    await configurator.ConfigureDefaultsAsync();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var tasks = await dbContext.Tasks.IgnoreQueryFilters().Where(i => i.ActualStatus == TaskLauncher.Common.Enums.TaskState.Created).ToListAsync();
    foreach(var task in tasks)
    {
        balancer.Enqueue("nonvip", new()
        {
            State = 0,
            Id = task.Id,
            TaskFilePath = task.TaskFile,
            Time = DateTime.Now,
            UserId = task.UserId
        });
    }
}

app.MapHangfireDashboard().RequireAuthorization("admin-policy");
app.Run();
