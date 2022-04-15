using RoundRobin;

namespace TaskLauncher.Common.Extensions;

/// <summary>
/// Extenze pro round robin algoritmus
/// </summary>
public static class RoundRobinExtensions
{
    /// <summary>
    /// Iteraci ziska dalsi druh polozky na zaklade aktualni polozky
    /// Round robin muze napriklad postupne vracet 1,1,1,1,2,2,3 pokud mam aktualne 1 a chci dostat hned dalsi polozku mimo aktualni 1, zavolam tuto metodu, ta vrati 2
    /// </summary>
    public static T GetNextItem<T>(this RoundRobinList<T> list, T current)
    {
        while (true)
        {
            var next = list.Next();
            if (!ReferenceEquals(next, current))
                return next;
        }
    }
}