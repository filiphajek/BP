using Microsoft.AspNetCore.Mvc;

namespace TaskLauncher.Api.Controllers.Base;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected ILogger logger;

    public BaseController(ILogger logger)
    {
        this.logger = logger;
    }

    protected string GetToken() => HttpContext.Request.Headers.Authorization.ToString().Split(' ').Last();

    protected BadRequestObjectResult BadRequest(string errorMessage) => base.BadRequest(new { error = errorMessage });
}
