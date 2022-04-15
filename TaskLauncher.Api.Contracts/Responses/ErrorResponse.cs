using TaskLauncher.Common.Models;

namespace TaskLauncher.Api.Contracts.Responses;

public record ErrorResponse
{
    public List<ErrorModel> Errors { get; set; } = new();
}

public record ErrorMessageResponse
{
    /// <summary>
    /// Chyba
    /// </summary>
    /// <example>Chybová zpáva</example>
    public string Error { get; }

    public ErrorMessageResponse(string error)
    {
        Error = error;
    }
}

