using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record ConfigResponse
{
    public string Key { get; set; }
    public ConstantTypes Type { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
}
