namespace TaskLauncher.ContainerLauncher.Extensions;

public static class EnumerableExtensions
{
    public static int IndexOf<T>(this IEnumerable<T> collection, T searchItem)
    {
        int index = 0;
        foreach (var item in collection)
        {
            if (EqualityComparer<T>.Default.Equals(item, searchItem))
                return index;
            index++;
        }
        return -1;
    }
}
