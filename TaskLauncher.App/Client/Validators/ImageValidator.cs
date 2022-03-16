using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace TaskLauncher.App.Client.Validators;

public class ImageValidator : ValidatorBase
{
    [Parameter]
    public override string Text { get; set; } = "Wrong url";

    protected override bool Validate(IRadzenFormComponent component)
    {
        var imageUrl = component.GetValue().ToString();
        return Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute);
    }
}
