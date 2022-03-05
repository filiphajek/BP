using Mapster;
using System.ComponentModel.DataAnnotations;

namespace TaskLauncher.WebApp.Client.Models;

public class TaskModel
{
    [Required(ErrorMessage = "Name is required field")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Description is required field")]
    public string Description { get; set; }
}