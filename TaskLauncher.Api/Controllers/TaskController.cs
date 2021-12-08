using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Controllers;

/// <summary>
/// Kontroler pro uzivatelskou aplikaci, vytvari a spravuje tasky
/// </summary>
[Authorize(Policy = "p-user-api-auth0")]
public class TaskController : BaseController
{
    private readonly IMapper mapper;
    private readonly ITaskRepository taskRepository;
    private readonly IFileStorageService fileStorageService;
    private readonly IFileRepository fileRepository;

    public TaskController(IMapper mapper,
        ITaskRepository taskRepository,
        IFileStorageService fileStorageService, 
        IFileRepository fileRepository,
        ILogger<TaskController> logger) : base(logger)
    {
        this.mapper = mapper;
        this.taskRepository = taskRepository;
        this.fileStorageService = fileStorageService;
        this.fileRepository = fileRepository;
    }

    /// <summary>
    /// Vytvoreni noveho tasku
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTaskAsync([FromForm] TaskCreateRequest request, IFormFile file)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var fileEntity = new FileEntity { Name = "task", UserId = userId };
        TaskEntity task = new() { UserId = userId, Files = new List<FileEntity> { fileEntity } };
        var result = await taskRepository.AddAsync(mapper.Map(request, task));

        using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync($"{userId}/{task.Id}/task", stream);
        }
        return Ok(mapper.Map<TaskDetailResponse>(result));
    }

    /// <summary>
    /// Smazani tasku spolecne s jeho soubory
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveTaskAsync(Guid id)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return BadRequest();
        
        if(task.Status == TaskState.InQueue || task.Status == TaskState.Running)
            return BadRequest();

        await fileStorageService.RemoveFileAsync($"{userId}/{task.Id}/task");
        await taskRepository.RemoveAsync(task);
        //taskRepository.ClearTrackedEntries();
        //await fileRepository.RemoveAsync(task.File);
        return Ok();
    }

    /// <summary>
    /// Akualizace udaju o tasku (jmeno, popis)
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateTaskAsync([FromBody] TaskUpdateRequest request)
    {
        var task = await taskRepository.GetAsync(new() { Id = request.Id });
        if (task is null)
            return NotFound();

        task.Name = request.Name;
        task.Description = request.Description;
        await taskRepository.UpdateAsync(task);
        return Ok();
    }

    /// <summary>
    /// Vraci vsechny uzivatelske tasky
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllTasksAsync()
    {
        var list = await taskRepository.GetAllAsync();
        return Ok(list.Select(mapper.Map<TaskResponse>));
    }

    /// <summary>
    /// Vraci detail tasku
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTaskDetailAsync(Guid id)
    {
        var task = await taskRepository.GetAsync(new() { Id = id });
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }
}