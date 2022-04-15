using Microsoft.AspNetCore.Mvc;

namespace TaskLauncher.App.Server.Controllers.Base;

/// <summary>
/// Abstraktni bazova trida pro vsechny kontrolery
/// </summary>
[Route("api/[controller]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected ILogger logger;

    public BaseController(ILogger logger)
    {
        this.logger = logger;
    }

    protected string GetToken() => HttpContext.Request.Headers.Authorization.ToString().Split(' ').Last();

    protected BadRequestObjectResult BadRequest(string errorMessage) => base.BadRequest(new { error = errorMessage });
}
