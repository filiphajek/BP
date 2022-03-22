using Cocona;

var app = CoconaApp.CreateBuilder().Build();
Random rnd = new(Guid.NewGuid().GetHashCode());

app.AddCommand("minutes", async (int min, int max, float? chance) =>
{
    await Execute(true, min, max);
    return GetResult(chance is null ? 0.75 : chance.Value);
});

app.AddCommand("seconds", async (int min, int max, float? chance) =>
{
    await Execute(false, min, max);
    return GetResult(chance is null ? 0.75 : chance.Value);
});

async Task Execute(bool minutes, int minCount, int maxCount)
{
    Console.WriteLine("Test containter started");
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
    using var file = File.OpenWrite("/app/tmp/task.txt");
    using StreamWriter streamWriter = new(file);
    var result = rnd.NextDouble();
    if (result < chanceToGoodResult)
    {
        streamWriter.Write("Good result");
        Console.WriteLine("Good result");
        return 1;
    }
    streamWriter.Write("Bad result");
    Console.WriteLine("Bad result");
    return -1;
}

app.Run();
