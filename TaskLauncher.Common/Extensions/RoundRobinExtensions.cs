using Mapster;
using RoundRobin;

namespace TaskLauncher.Common.Extensions;

public static class RoundRobinExtensions
{
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