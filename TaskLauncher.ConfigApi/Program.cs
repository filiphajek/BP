using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using RawRabbit;
using RawRabbit.Context;
using RawRabbit.vNext;
using System.Xml.Linq;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Messages;
using TaskLauncher.Common.Services;
using TaskLauncher.ConfigApi;

// TODO rate limiting https://www.youtube.com/watch?v=GQAgh_z1rHY&ab_channel=NickChapsas

static void ConfigInit(string path)
{
    if (File.Exists(path) && XElement.Load(path) is not null)
        return;

    var root = new XElement("Values");
    root.Save(path);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRawRabbit(cfg => cfg.AddJsonFile("rawrabbit.json"));

builder.Services.Configure<StorageConfig>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<IConfigFileEditor, ConfigFileEditor>();
ConfigInit(builder.Configuration["Storage:Path"]);

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

builder.Services.AddSingleton(services =>
{
    var config = services.GetRequiredService<IConfiguration>();
    var tmp = new StorageConfiguration();
    config.Bind(nameof(StorageConfiguration), tmp);
    return tmp;
});
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<FileDeletionRoutine>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapPost("/api/config", async (IConfigFileEditor fileEditor, IBusClient<MessageContext> busClient, AddConfigValueRequest request) =>
{
    fileEditor.Write(request.Name, request.Value);
    //TODO zmenit hangfire schedule

    //musi to tu byt aby ostatni aplikace dokazali zaregistrovat zmenu v konfiguraci
    await busClient.PublishAsync(new ConfigChangedMessage { Name = request.Name, Value = request.Value }, configuration: config =>
    {
        config.WithRoutingKey("config-hello-que.#");
        config.WithExchange(exchange =>
        {
            exchange.WithName("config-hello-exchange");
        });
    });
}).RequireAuthorization();

app.MapGet("/api/config", (IConfigFileEditor fileEditor, string? name) =>
{
    if (name is not null)
    {
        var val = fileEditor.GetValue(name);
        return Results.Ok(new { value = val });
    }
    return Results.Ok(fileEditor.GetConfig());
}).RequireAuthorization();

app.MapGet("/api/schedule", (IBackgroundJobClient client, FileDeletionRoutine routine) =>
{
    //alternativa http call
    client.Schedule(() => routine.Handle(), TimeSpan.FromSeconds(20));
}).AllowAnonymous();

app.MapHangfireDashboard().AllowAnonymous(); //testing
app.Run();
