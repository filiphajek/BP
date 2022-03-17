using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.DAL.Entities;

public record ConfigEntity
{
    public string Key { get; set; }
    public ConstantTypes Type { get; set; } = ConstantTypes.Number;
    public string Value { get; set; }
    public string Description { get; set; }
}