namespace TaskLauncher.ManagementApi
{
    public interface IConfigurationFile
    {
        Dictionary<string, string> GetConfig();
        string? GetValue(string name);
        void Write(string name, string value);
    }
}