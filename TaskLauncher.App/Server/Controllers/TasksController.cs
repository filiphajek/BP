﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Pridava k task endpointum dalsi endpoint pro ziskani souboru
/// </summary>
public class TasksController : BaseController
{
    private readonly AppDbContext context;
    private readonly IFileStorageService fileStorageService;

    public TasksController(AppDbContext context, ILogger<TasksController> logger, IFileStorageService fileStorageService) : base(logger)
    {
        this.context = context;
        this.fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Vrací výsledný soubor dané úlohy
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [Authorize(Policy = Constants.Policies.CanViewTaskPolicy)]
    [ProducesResponseType(typeof(FileStreamResult), 200, contentType: "application/octet-stream")]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400, contentType: "application/json")]
    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> DownloadFileAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        //pokud je to admin
        if (User.IsInRole(Constants.Roles.Admin))
        {
            var downloadedTask = await context.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
            if (downloadedTask is null)
                return NotFound();

            if (downloadedTask.ActualStatus == TaskState.Downloaded || downloadedTask.ActualStatus == TaskState.FinishedSuccess || downloadedTask.ActualStatus == TaskState.FinishedFailure)
                return await DownloadFileAsync(downloadedTask);
            return Unauthorized();
        }

        //pokud to je user
        var task = await context.Tasks.SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        if (task.ActualStatus == TaskState.FinishedSuccess || task.ActualStatus == TaskState.FinishedFailure)
        {
            task.ActualStatus = TaskState.Downloaded;
            context.Update(task);
            await context.Events.AddAsync(new() { Task = task, Status = TaskState.Downloaded, Time = DateTime.Now, UserId = userId });
            await context.SaveChangesAsync();
        }

        if (task.ActualStatus == TaskState.Downloaded)
            return await DownloadFileAsync(task);
        return BadRequest(new ErrorMessageResponse("Task is not in finished state"));
    }

    /// <summary>
    /// Stazeni souboru
    /// </summary>
    private async Task<IActionResult> DownloadFileAsync(TaskEntity task)
    {
        MemoryStream stream = new();
        try
        {
            await fileStorageService.DownloadFileAsync(task.ResultFile, stream);
        }
        catch 
        {
            return NotFound();
        }
        return File(stream, "application/octet-stream", task.Name);
    }
}
