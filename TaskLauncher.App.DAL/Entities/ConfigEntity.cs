namespace TaskLauncher.App.DAL.Entities;

public record ConfigEntity
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
    public bool CanDelete { get; set; } = true;
}