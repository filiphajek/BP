using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace TaskLauncher.ConfigApi;

public class ConfigFileEditor : IConfigFileEditor
{
    private readonly StorageConfig options;
    private readonly XElement rootElement;

    public ConfigFileEditor(IOptions<StorageConfig> options)
    {
        this.options = options.Value;
        rootElement = XDocument.Load(options.Value.Path).Root!;
    }

    public string? GetValue(string name)
    {
        return rootElement.Elements().SingleOrDefault(i => i.Name.LocalName == name)?.Value;
    }

    public Config GetConfig()
    {
        var result = new Config();
        foreach (var item in rootElement.Elements())
        {
            result.Values.Add(item.Name.LocalName, item.Value);
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
