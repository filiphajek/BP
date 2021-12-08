using TaskLauncher.Common.Models;

namespace TaskLauncher.Api.Contracts.Responses;

public record ErrorResponse
{
    public List<ErrorModel> Errors { get; set; } = new();
}