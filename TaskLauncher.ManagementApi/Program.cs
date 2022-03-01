using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using RawRabbit.Context;
using RawRabbit.vNext;
using System.Xml.Linq;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Services;
using TaskLauncher.Common.TypedRawRabbit;
using TaskLauncher.ManagementApi;

// TODO rate limiting https://www.youtube.com/watch?v=GQAgh_z1rHY&ab_channel=NickChapsas

static void ConfigInit(string path)
{
    if (File.Exists(path) && XElement.Load(path) is not null)
        return;

    var root = new XElement("Values");
    root.Add(new XElement("autofileremove", "200"));
    root.Save(path);
}

var builder = WebApplication.CreateBuilder(args);

//raw rabbit
builder.Services.AddRawRabbit(cfg => cfg.AddJsonFile("rawrabbit.json"));
builder.Services.InstallTypedRawRabbit<Program>();

//konfigurace
builder.Services.Configure<TaskLauncher.ManagementApi.StorageConfiguration>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<SubscriberConfiguration>(builder.Configuration.GetSection("PublishMessage"));
builder.Services.Configure<TaskLauncher.Common.Configuration.StorageConfiguration>(builder.Configuration.GetSection(nameof(TaskLauncher.Common.Configuration.StorageConfiguration)));

builder.Services.AddSingleton<IConfigurationFile, ConfigurationFile>();
ConfigInit(builder.Configuration["ConfigStorage:Path"]);

//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securitySchema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, new[] { JwtBearerDefaults.AuthenticationScheme } }
    });
});

//autorizace
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Authority = "https://dev-8nhuxay1.us.auth0.com/";
    options.Audience = "https://wutshot-test-api.com";
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .RequireRole("admin")
    .Build();
});

//hangfire
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

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddRoutines<Program>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapPost("/api/config", async (IRecurringJobManager client, FileDeletionRoutine routine, 
    IConfigurationFile fileEditor, IDefaultPublisher publisher, AddConfigValueRequest request) =>
{
    fileEditor.Write(request.Name, request.Value);

    if(request.Name == "autofileremove")
    {
        //zmena hangfire schedule
        client.RemoveIfExists(nameof(FileDeletionRoutine));
        client.AddOrUpdate(nameof(FileDeletionRoutine), () => routine.Perform(), Cron.Daily);
    }

    await publisher.PublishAsync(new ConfigChangedMessage { Name = request.Name, Value = request.Value });

}).AllowAnonymous();

app.MapGet("/api/config", (IConfigurationFile fileEditor, string? name) =>
{
    if (name is not null)
    {
        var val = fileEditor.GetValue(name);
        return Results.Ok(new { value = val });
    }
    return Results.Ok(new { values = fileEditor.GetConfig() });
}).AllowAnonymous();

app.MapGet("/api/schedule", (IBackgroundJobClient client, FileDeletionRoutine routine) =>
{
    client.Schedule(() => routine.Perform(), TimeSpan.FromSeconds(20));
}).AllowAnonymous();

app.MapHangfireDashboard().AllowAnonymous(); //testing
app.Run();
