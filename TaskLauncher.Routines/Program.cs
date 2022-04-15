using Hangfire;
using TaskLauncher.App.DAL.Installers;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Services;
using TaskLauncher.Routines.Extensions;
using TaskLauncher.Routines.Routines;

var builder = WebApplication.CreateBuilder(args);

//pristup do google bucket storage
builder.Services.Configure<StorageConfiguration>(builder.Configuration.GetSection(nameof(StorageConfiguration)));
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

//hangfire
builder.Services.AddHangfire<Program>(builder.Configuration);

//database
new DatabaseInstaller().Install(builder.Services, builder.Configuration);
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

//for testing:
app.UseHttpsRedirection();
app.UseRouting();
app.UseHangfireDashboard("/hangfire");
app.MapHangfireDashboard();

//inicializace rutin
using (var scope = app.Services.CreateScope())
{
    var jobClient = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var routine = scope.ServiceProvider.GetRequiredService<FileDeletionRoutine>();
    jobClient.RemoveIfExists(nameof(FileDeletionRoutine));
    jobClient.AddOrUpdate(nameof(FileDeletionRoutine), () => routine.Perform(), Cron.Daily);
    jobClient.Trigger(nameof(FileDeletionRoutine));
}

app.Run();
