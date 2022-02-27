namespace TaskLauncher.ManagementApi
{
    public interface IConfigFileEditor
    {
        Config GetConfig();
        string? GetValue(string name);
        void Write(string name, string value);
    }
}