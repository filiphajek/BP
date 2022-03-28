namespace TaskLauncher.Api.Contracts.Responses;

public record TaskStatResponse(bool IsVip, string TaskName, TimeSpan TimeInQueue, TimeSpan CpuTime);
