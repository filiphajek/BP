using System.Net.Http.Json;
using TaskLauncher.Common.Configuration;

namespace TaskLauncher.Common.Services;

/// <summary>
/// Pro ziskani autorizacniho tokenu
/// Chybi implementace refresh tokenu
/// </summary>
public class TokenProvider
{
    /// <summary>
    /// Pomocna trida pro deserializaci
    /// </summary>
    private class AccessTokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    private readonly Auth0Configuration configuration;

    public TokenProvider(Auth0Configuration configuration)
    {
        this.configuration = configuration;
    }

    //string testToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImI1Wk1YcmFOOE82YUlxTUJtZnhDViJ9.eyJpc3MiOiJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vIiwic3ViIjoiMU1CaE5CUHFmU3M4RllsYUhvRkxlMnVSd2E1QlY1UWFAY2xpZW50cyIsImF1ZCI6Imh0dHBzOi8vd3V0c2hvdC10ZXN0LWFwaS5jb20iLCJpYXQiOjE2Mzg3NTQ5NTYsImV4cCI6MTYzODg0MTM1NiwiYXpwIjoiMU1CaE5CUHFmU3M4RllsYUhvRkxlMnVSd2E1QlY1UWEiLCJndHkiOiJjbGllbnQtY3JlZGVudGlhbHMifQ.Wg859OZuFmVUQ26SqOjM9EbiKmYM12z110qOj6grhAV6F__XP4KG5vkrO-3aGJ6d-5WGbzDZNWq_mXQuMBIhRE7P8XuC7QdafocYN6D0xy6giKWG3Yd4WFOjIbzyxDz55-2uOKrmD7go9aBqy11lqnuN4TqKF7tEM614dl59PgW0rj5GKOJ3gxzKLWYMtUcdsTKaag508uzYTDKrbOMqQ0EcrX51cKTUjOlQ540nMeKSGhUwLDwUlLgg18ZfYWxFV8J4T4I8feSTvwF5GUNEOufrMsE_USPIDpN0V12eUHBBnsvpR3LukVB8m0cvokSVhAp8a1fN9OBlpUOY5dsikQ";
    string testToken = "";

    public async Task<string> Authorize()
    {
        if(!string.IsNullOrEmpty(testToken))
        {
           return testToken;
        }

        HttpClient client = new();
        var response = await client.PostAsJsonAsync(configuration.Domain, new
        {
            grant_type = "client_credentials",
            client_id = configuration.ClientId,
            client_secret = configuration.ClientSecret,
            audience = configuration.Audience
        });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
            if (result is null)
                throw new ApplicationException($"Cant deserialize response message: {await response.Content.ReadAsStringAsync()}");
            
            //todo refresh token
            DateTime expirationTime = DateTimeOffset.FromUnixTimeSeconds(result.expires_in).DateTime;
            testToken = result.access_token;
            return result.access_token;
        }
        throw new ApplicationException($"Bad request to auth endpoint: {await response.Content.ReadAsStringAsync()}");
    }
}