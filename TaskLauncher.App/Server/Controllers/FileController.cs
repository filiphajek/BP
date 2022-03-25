using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Controllers;

public class FileController : BaseController
{
    private readonly AppDbContext context;
    private readonly IFileStorageService fileStorageService;

    public FileController(AppDbContext context, ILogger<FileController> logger, IFileStorageService fileStorageService) : base(logger)
    {
        this.context = context;
        this.fileStorageService = fileStorageService;
    }

    [Authorize(Policy = TaskLauncherPolicies.CanViewTaskPolicy)]
    [HttpGet]
    public async Task<IActionResult> DownloadFileAsync(Guid taskId)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        TaskEntity? task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == taskId);
        if (User.IsInRole(TaskLauncherRoles.Admin))
            task = await context.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == taskId);

        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.FinishedSuccess || task.ActualStatus == TaskState.FinishedFailure)
        {
            if (User.IsInRole(TaskLauncherRoles.User))
            {
                task.ActualStatus = TaskState.Downloaded;
                context.Update(task);
                await context.Events.AddAsync(new() { Task = task, Status = TaskState.Downloaded, Time = DateTime.Now, UserId = userId });
                await context.SaveChangesAsync();
            }
            MemoryStream stream = new();
            await fileStorageService.DownloadFileAsync(task.ResultFile, stream);
            return File(stream, "application/octet-stream", task.Name);
        }

        if (task.ActualStatus == TaskState.Downloaded)
        {
            MemoryStream stream = new();
            await fileStorageService.DownloadFileAsync(task.ResultFile, stream);
            return File(stream, "application/octet-stream", task.Name);
        }
        return BadRequest();
    }
}
