using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace TaskLauncher.App.Client.Validators;

/// <summary>
/// Validator kontroluje zda v textboxu nejsou znaky '( )'
/// </summary>
public class InvalidCharactersValidator : ValidatorBase
{
    [Parameter]
    public override string Text { get; set; } = "Invalid characters are: '( )'";

    protected override bool Validate(IRadzenFormComponent component)
    {
        var text = component.GetValue().ToString();
        if(text!.Contains('(') || text!.Contains(')'))
            return false;
        return true;
    }
}

