using Cocona;
using System.Globalization;

var app = CoconaApp.CreateBuilder().Build();
Random rnd = new(Guid.NewGuid().GetHashCode());

app.AddCommand("minutes", async (int min, int max, string? chance) =>
{
    var tmpChance = GetChanceValue(chance);
    Console.WriteLine($"Test containter started with parameters: Command=minutes, Min={min}, Max={max}, Chance={tmpChance}");
    await Execute(true, min, max);
    return GetResult(tmpChance);
});

app.AddCommand("seconds", async (int min, int max, string? chance) =>
{
    var tmpChance = GetChanceValue(chance);
    Console.WriteLine($"Test containter started with parameters: Command=seconds, Min={min}, Max={max}, Chance={tmpChance}");
    await Execute(false, min, max);
    return GetResult(tmpChance);
});

float GetChanceValue(string? value)
{
    if (string.IsNullOrEmpty(value))
        return 0.75f;
    if(float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        return result;
    return 0.75f;
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
