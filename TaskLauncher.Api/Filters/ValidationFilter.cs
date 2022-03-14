using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Api.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(i => i.Value.Errors.Any())
                .ToDictionary(i => i.Key, j => j.Value.Errors.Select(x => x.ErrorMessage).ToArray());

            var response = new ErrorResponse();
            foreach (var error in errors)
            {
                response.Errors.AddRange(error.Value.Select(i => new ErrorModel(error.Key, i)));
            }

            context.Result = new BadRequestObjectResult(response);
            return;
        }
        await next();
    }
}