using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.Client.Validators;

public class ConfigValidator : ValidatorBase
{
    [Parameter]
    public override string Text { get; set; } = "Wrong type";

    [Parameter]
    public ConstantTypes Type { get; set; }

    protected override bool Validate(IRadzenFormComponent component)
    {
        var value = component.GetValue().ToString();
        switch (Type)
        {
            case ConstantTypes.Number:
                if (!int.TryParse(value, out var parsedInt))
                {
                    Text = "This is not a number";
                    return false;
                }
                if(parsedInt <= 0)
                {
                    Text = "Number has to be higher than 0";
                    return false;
                }
                break;
            case ConstantTypes.String:
                break;
            default:
                throw new NotSupportedException(nameof(Type));
        }
        return true;
    }
}
