using Cocona;

var app = CoconaApp.CreateBuilder().Build();
Random rnd = new(Guid.NewGuid().GetHashCode());

app.AddCommand("minutes", async (int min, int max, int? chance) =>
{
    //kontrola parametru
    if (min < 0 || max < min)
    {
        Console.WriteLine("Wrong min or max parameter");
        return -1;
    }
    if (!CheckChanceParameter(chance))
    {
        Console.WriteLine("Chance parameter should be number from interval <0,100>");
        return -2;
    }

    var tmpChance = GetChanceValue(chance);
    Console.WriteLine($"Test containter started with parameters: Command=minutes, Min={min}, Max={max}, Chance={tmpChance}");
    //spusteni simulace
    await Execute(true, min, max);
    return GetResult(tmpChance);
});

app.AddCommand("seconds", async (int min, int max, int? chance) =>
{
    //kontrola parametru
    if (min < 0 || max < min)
    {
        Console.WriteLine("Wrong min or max parameter");
        return -1;
    }
    if (!CheckChanceParameter(chance))
    {
        Console.WriteLine("Chance parameter should be number from interval <0,100>");
        return -2;
    }

    var tmpChance = GetChanceValue(chance);
    Console.WriteLine($"Test containter started with parameters: Command=seconds, Min={min}, Max={max}, Chance={tmpChance}");
    //spusteni simulace
    await Execute(false, min, max);
    return GetResult(tmpChance);
});

bool CheckChanceParameter(int? chance)
{
    if (!chance.HasValue)
        return true;

    return chance.Value >= 0 && chance.Value <= 100;
}

float GetChanceValue(int? value)
{
    if (!value.HasValue)
        return 0.75f;
    return (float)value.Value / 100;
}

async Task Execute(bool minutes, int minCount, int maxCount)
{
    var next = rnd.Next(minCount, maxCount);
    if (minutes)
        await Task.Delay(TimeSpan.FromMinutes(next));
    else
        await Task.Delay(TimeSpan.FromSeconds(next));

    var tmp = minutes ? "minutes" : "seconds";
    Console.WriteLine($"Test containter finished after {next} {tmp}");
}

int GetResult(double chanceToGoodResult)
{
    var result = rnd.NextDouble();
    if (result < chanceToGoodResult)
    {
        File.AppendAllText("/app/tmp/task.txt", Environment.NewLine + "Good result");
        Console.WriteLine("Good result");
        return 1;
    }
    File.AppendAllText("/app/tmp/task.txt", Environment.NewLine + "Bad result");
    Console.WriteLine("Bad result");
    return 0;
}

app.Run();
