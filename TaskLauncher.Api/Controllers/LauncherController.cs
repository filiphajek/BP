using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Controllers;

/// <summary>
/// Enpointy pouze pro launcher aplikaci
/// </summary>
[Authorize(Policy = "launcher")]
public class LauncherController : BaseController
{
    private readonly IFileStorageService fileStorageService;
    private readonly IFileRepository fileRepository;
    private readonly ITaskRepository taskRepository;

    public LauncherController(ILogger<LauncherController> logger, 
        IFileStorageService fileStorageService, 
        IFileRepository fileRepository,
        ITaskRepository taskRepository) 
        : base(logger)
    {
        this.fileStorageService = fileStorageService;
        this.fileRepository = fileRepository;
        this.taskRepository = taskRepository;
    }

    /// <summary>
    /// Stazeni task souboru (parametr pro spusteni image)
    /// </summary>
    [HttpGet("/launcher/task")]
    public async Task<IActionResult> DownloadTaskFileAsync(string userId, Guid id)
    {
        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return NotFound();

        MemoryStream stream = new();
        await fileStorageService.DownloadFileAsync($"{userId}/{task.Id}/task", stream);

        return File(stream, "application/octet-stream", "task");
    }

    /// <summary>
    /// Nahrani souboru s vysledkem
    /// </summary>
    [Authorize(Policy = "launcher")]
    [HttpPost("/launcher/task")]
    public async Task<IActionResult> UploadResultFileAsync(string userId, Guid id, IFormFile file)
    {
        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return BadRequest();

        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync($"{userId}/{task.Id}/result", stream);
        }

        if(task.Files.Count < 2)
            await taskRepository.AddFile(task, new() { Name = "result", Task = task, UserId = userId });
        return Ok();
    }

    /// <summary>
    /// Aktualizace stavu tasku
    /// </summary>
    [HttpPut("/launcher/task")]
    public async Task<IActionResult> UpdateTaskStatusAsync([FromBody] TaskStatusUpdateRequest request)
    {
        var task = await taskRepository.GetAsync(new() { Id = request.Id });
        if (task is null)
            return NotFound();

        if (request.State == TaskState.InQueue)
        {
            task.Start = request.Time;
            task.End = null;
        }
        if (request.State == TaskState.Finished)
            task.End = request.Time;

        task.Status = request.State;
        await taskRepository.UpdateAsync(task);
        return Ok();
    }
}
