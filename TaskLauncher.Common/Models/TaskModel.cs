﻿using TaskLauncher.Common.Enums;

namespace TaskLauncher.Common.Models;

public class TaskModel
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public TaskState State { get; set; } = TaskState.Created;
    public DateTime Time { get; set; }
    public string TaskFilePath { get; set; }
    public string ResultFilePath { get; set; }
}
