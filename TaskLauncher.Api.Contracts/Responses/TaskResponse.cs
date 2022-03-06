using System.ComponentModel.DataAnnotations;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.Api.Contracts.Responses;

public record AuthResponse(string access_token, string refresh_token, string id_token, string token_type, int expires_in);
public record RefreshTokenResponse(string access_token, string scope, string token_type, int expires_in);

public record TokenBalanceResponse
{
    public double CurrentAmount { get; set; }
    public DateTime LastAdded { get; set; }
}


public record TaskResponse2
{
    [Key]
    public Guid Id { get; set; }
    public string TaskFile { get; set; }
    public string ResultFile { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TaskState ActualStatus { get; set; } = TaskState.Created;
}

public record TaskResponse
{
    [Key]
    public Guid Id { get; set; }
    public string TaskFile { get; set; }
    public string ResultFile { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TaskState ActualStatus { get; set; } = TaskState.Created;
}

public record TaskDetailResponse : TaskResponse
{
    public List<EventResponse> Events { get; set; } = new();
}

public record EventResponse
{
    public TaskState Status { get; set; }
    public DateTime Time { get; set; }
}

public record PaymentResponse
{
    public TaskResponse Task { get; set; }
    public DateTime Time { get; set; }
    public double Price { get; set; }
}