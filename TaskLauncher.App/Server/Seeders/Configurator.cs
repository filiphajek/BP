using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.Server.Seeders;

/// <summary>
/// Trida inicializuje vsechny konfiguracni promenne, pokud jiz v databazi neexistuji
/// </summary>
public class Configurator
{
    private readonly AppDbContext dbContext;

    public Configurator(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task ConfigureDefaultsAsync()
    {
        await AddConfigValue(Constants.Configuration.FileRemovalRoutine, ConstantTypes.Number, "Po jake dobe se smazou soubory (v dnech)", "7");
        await AddConfigValue(Constants.Configuration.TaskTimeout, ConstantTypes.Number, "Po jak dlouhe dobe se ma zrusit task (v minutach)", "45");
        await AddConfigValue(Constants.Configuration.VipTaskPrice, ConstantTypes.Number, "Cena tasku spustena vip uzivatelem v tokenech", "2");
        await AddConfigValue(Constants.Configuration.NormalTaskPrice, ConstantTypes.Number, "Normalni cena tasku v tokenech", "1");
        await AddConfigValue(Constants.Configuration.StartTokenBalance, ConstantTypes.Number, "Zakladni pocet tokenu po registraci", "200");
        await dbContext.SaveChangesAsync();
    }

    private async Task AddConfigValue(string key, ConstantTypes type, string description, string value)
    {
        if (!await dbContext.Configs.AnyAsync(i => i.Key == key))
        {
            await dbContext.AddAsync(new ConfigEntity { Type = type, Description = description, Key = key, Value = value });
        }
    }
}
