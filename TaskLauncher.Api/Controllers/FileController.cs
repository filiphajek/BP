using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Controllers;

/// <summary>
/// Stahovani, nahravani souboru klientem, pouze pro uzivatelskou aplikaci
/// </summary>
[Authorize(Policy = "p-user-api-auth0")]
public class FileController : BaseController
{
    private readonly IFileStorageService fileStorageService;
    private readonly ITaskRepository taskRepository;
    private readonly IFileRepository fileRepository;

    public FileController(ILogger<FileController> logger, 
        IFileStorageService fileStorageService,
        ITaskRepository taskRepository,
        IFileRepository fileRepository) 
        : base(logger)
    {
        this.fileStorageService = fileStorageService;
        this.taskRepository = taskRepository;
        this.fileRepository = fileRepository;
    }

    /// <summary>
    /// Aktualizace souboru
    /// </summary>
    [HttpPost("{id:guid}")]
    public async Task<IActionResult> UpdateFileAsync(Guid id, IFormFile file)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var fileEntity = await fileRepository.GetAsync(new() { Id = id });
        if (fileEntity is null)
            return BadRequest();

        await fileStorageService.RemoveFileAsync($"{userId}/{fileEntity.Task.Id}/{fileEntity.Name}");
        
        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync($"{userId}/{fileEntity.Task.Id}/{file.FileName}", stream);
        }

        fileEntity.Name = file.FileName;
        await fileRepository.UpdateAsync(fileEntity);
        return Ok();
    }

    /// <summary>
    /// Stazeni vysledneho souboru
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> DownloadResultFileAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return NotFound();

        MemoryStream stream = new();
        await fileStorageService.DownloadFileAsync($"{userId}/{task.Id}/result", stream);
        return File(stream, "application/octet-stream", task.Name);
    }
}
