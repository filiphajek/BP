namespace TaskLauncher.App.Server.Hub;

/// <summary>
/// In memory uloziste signalr spojeni, inspirovano z oficialni dokumentace SignalR
/// Prevzato z oficialni dokumentace https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/mapping-users-to-connections
/// </summary>
public class SignalRMemoryStorage
{
    private readonly Dictionary<string, HashSet<string>> clientsConnections = new();

    public void Add(string key, string connectionId)
    {
        lock (clientsConnections)
        {
            if (!clientsConnections.TryGetValue(key, out var conns))
            {
                conns = new HashSet<string>();
                clientsConnections.Add(key, conns);
            }

            lock (conns)
            {
                conns.Add(connectionId);
            }
        }
    }

    public void Remove(string userId)
    {
        lock (clientsConnections)
        {
            if (!clientsConnections.TryGetValue(userId, out var conns))
            {
                return;
            }

            lock (clientsConnections)
            {
                clientsConnections.Clear();
            }
        }
    }

    public void Remove(string userId, string connectionId)
    {
        lock (clientsConnections)
        {
            if (!clientsConnections.TryGetValue(userId, out var conns))
            {
                return;
            }

            lock (clientsConnections)
            {
                clientsConnections.Remove(connectionId);

                if (clientsConnections.Count == 0)
                {
                    conns.Remove(userId);
                }
            }
        }
    }

    public IEnumerable<string> GetConnections(string key)
        => clientsConnections.TryGetValue(key, out var conns) ? conns : Enumerable.Empty<string>();

    public void RemoveDeadConnections()
    {
        List<string> tmp = new();
        foreach (var conn in clientsConnections)
        {
            if (conn.Value.Count == 0)
            {
                tmp.Add(conn.Key);
            }
        }

        foreach (var conn in tmp)
        {
            clientsConnections.Remove(conn);
        }
    }
}
