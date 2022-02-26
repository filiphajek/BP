namespace TaskLauncher.ConfigApi
{
    public interface IConfigFileEditor
    {
        Config GetConfig();
        string? GetValue(string name);
        void Write(string name, string value);
    }
}