using Hangfire;
using Hangfire.SqlServer;

namespace TaskLauncher.Routines.Extensions;

/// <summary>
/// Pridava hangfire do DI
/// </summary>
public static class HangfireExtensions
{
    public static void AddHangfire<TAssembly>(this IServiceCollection services, IConfiguration config)
    {
        services.AddRoutines<TAssembly>();
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(config.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer();
    }
}