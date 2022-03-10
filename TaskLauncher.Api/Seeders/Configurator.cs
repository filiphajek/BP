using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.Seeders;

public class Configurator
{
    private readonly AppDbContext dbContext;

    public Configurator(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task ConfigureDefaultsAsync()
    {
        await AddConfigValue("autofileremove", "Po jake dobe se smazou soubory (v dnechs)", "7");
        await AddConfigValue("tasktimeout", "Po jak dlouhe dobe se ma zrusit task (v hodinach)", "1");
        await dbContext.SaveChangesAsync();
    }

    private async Task AddConfigValue(string key, string description, string value)
    {
        if (!await dbContext.Configs.AnyAsync(i => i.Key == key))
        {
            await dbContext.AddAsync(new ConfigEntity { CanDelete = false, Description = description, Key = key, Value = value });
        }
    }
}
