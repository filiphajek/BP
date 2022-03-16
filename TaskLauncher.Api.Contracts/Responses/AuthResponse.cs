namespace TaskLauncher.Api.Contracts.Responses;

public record AuthResponse(string access_token, string refresh_token, string id_token, string token_type, int expires_in);
