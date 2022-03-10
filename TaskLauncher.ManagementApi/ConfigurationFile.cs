using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace TaskLauncher.ManagementApi;

public class ConfigurationFile : IConfigurationFile
{
    private readonly StorageFileConfiguration options;
    private readonly XElement rootElement;

    public ConfigurationFile(IOptions<StorageFileConfiguration> options)
    {
        this.options = options.Value;
        rootElement = XDocument.Load(options.Value.Path).Root!;
    }

    public string? GetValue(string name)
    {
        return rootElement.Elements().SingleOrDefault(i => i.Name.LocalName == name)?.Value;
    }

    public Dictionary<string, string> GetConfig()
    {
        var result = new Dictionary<string, string>();
        foreach (var item in rootElement.Elements())
        {
            result.Add(item.Name.LocalName, item.Value);
        }
        return result;
    }

    public void Write(string name, string value)
    {
        lock (rootElement)
        {
            var element = rootElement.Elements().SingleOrDefault(i => i.Name.LocalName == name);
            if (element is not null)
            {
                element.Value = value;
                rootElement.Save(options.Path);
                return;
            }

            rootElement.Add(new XElement(name, value));
            rootElement.Save(options.Path);
        }
    }
}
