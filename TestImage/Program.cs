public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Test container start");

        Console.WriteLine("0%");
        await Task.Delay(2000);
        Console.WriteLine("25%");
        await Task.Delay(2000);
        Console.WriteLine("50%");
        await Task.Delay(2000);
        Console.WriteLine("75%");
        await Task.Delay(2000);
        Console.WriteLine("100%");

        Console.WriteLine("Test container end");
    }
}

