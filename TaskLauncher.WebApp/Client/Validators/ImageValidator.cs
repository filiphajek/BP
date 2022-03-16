using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Text.RegularExpressions;

namespace TaskLauncher.WebApp.Client.Validators;

public class ImageValidator : ValidatorBase
{
    private static Regex extensionRegex = new(".(jpg|jpeg|png)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Parameter]
    public override string Text { get; set; } = "Wrong url";

    protected override bool Validate(IRadzenFormComponent component)
    {
        var imageUrl = component.GetValue().ToString();
        return Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute);
    }
}
