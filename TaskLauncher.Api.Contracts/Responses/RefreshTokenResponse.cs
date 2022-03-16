namespace TaskLauncher.Api.Contracts.Responses;

public record RefreshTokenResponse(string access_token, string scope, string token_type, int expires_in);
