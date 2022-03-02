using Swashbuckle.AspNetCore.Filters;
using TaskLauncher.Api.Contracts.Requests;

namespace TaskLauncher.Api.Contracts.SwaggerExamples;

public class CookieLessLoginRequestExample : IExamplesProvider<CookieLessLoginRequest>
{
    public CookieLessLoginRequest GetExamples() => new("testuser@example.com", "Password123*");
}