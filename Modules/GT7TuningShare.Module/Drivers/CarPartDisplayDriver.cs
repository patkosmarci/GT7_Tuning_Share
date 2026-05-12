using GT7TuningShare.Module.Models;
using GT7TuningShare.Module.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.Views;

namespace GT7TuningShare.Module.Drivers;

public sealed class CarPartDisplayDriver : ContentPartDisplayDriver<CarPart>
{
    public override IDisplayResult Display(CarPart part, BuildPartDisplayContext context)
    {
        return Initialize<CarPartViewModel>("CarPart", model => Bind(model, part))
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
    }

    public override IDisplayResult Edit(CarPart part, BuildPartEditorContext context)
    {
        return Initialize<CarPartViewModel>("CarPart_Edit", model => Bind(model, part));
    }

    public override async Task<IDisplayResult> UpdateAsync(CarPart part, UpdatePartEditorContext context)
    {
        var vm = new CarPartViewModel();
        await context.Updater.TryUpdateModelAsync(vm, Prefix);

        part.GameId = vm.GameId;
        part.Make = vm.Make ?? string.Empty;
        part.ShortName = vm.ShortName ?? string.Empty;
        part.Drivetrain = vm.Drivetrain ?? string.Empty;
        part.Category = vm.Category ?? string.Empty;

        return Edit(part, context);
    }

    private static void Bind(CarPartViewModel model, CarPart part)
    {
        model.GameId = part.GameId;
        model.Make = part.Make;
        model.ShortName = part.ShortName;
        model.Drivetrain = part.Drivetrain;
        model.Category = part.Category;
    }
}
