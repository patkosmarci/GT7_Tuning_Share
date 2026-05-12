using GT7TuningShare.Module.Models;
using GT7TuningShare.Module.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.Views;
using YesSql;

namespace GT7TuningShare.Module.Drivers;

public sealed class CarSetupPartDisplayDriver : ContentPartDisplayDriver<CarSetupPart>
{
    private readonly ISession _session;

    public CarSetupPartDisplayDriver(ISession session)
    {
        _session = session;
    }

    public override IDisplayResult Display(CarSetupPart part, BuildPartDisplayContext context)
    {
        return Initialize<CarSetupPartViewModel>("CarSetupPart", model => Bind(model, part))
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
    }

    public override IDisplayResult Edit(CarSetupPart part, BuildPartEditorContext context)
    {
        return Initialize<CarSetupPartViewModel>("CarSetupPart_Edit", async model =>
        {
            Bind(model, part);
            model.AvailableCars = await LoadCarsAsync();
        });
    }

    public override async Task<IDisplayResult> UpdateAsync(CarSetupPart part, UpdatePartEditorContext context)
    {
        var vm = new CarSetupPartViewModel();
        await context.Updater.TryUpdateModelAsync(vm, Prefix);

        part.CarContentItemId = vm.CarContentItemId ?? string.Empty;

        part.PowerLevel = vm.PowerLevel;
        part.WeightReduction = vm.WeightReduction;
        part.Ballast = vm.Ballast;
        part.BallastPosition = vm.BallastPosition;
        part.PowerRestrictor = vm.PowerRestrictor;

        part.FrontTires = vm.FrontTires ?? string.Empty;
        part.RearTires = vm.RearTires ?? string.Empty;

        part.FrontDownforce = vm.FrontDownforce;
        part.RearDownforce = vm.RearDownforce;

        part.RideHeightFront = vm.RideHeightFront;
        part.AntiRollBarFront = vm.AntiRollBarFront;
        part.SpringRateFront = vm.SpringRateFront;
        part.DamperCompressionFront = vm.DamperCompressionFront;
        part.DamperExtensionFront = vm.DamperExtensionFront;
        part.CamberFront = vm.CamberFront;
        part.ToeFront = vm.ToeFront;

        part.RideHeightRear = vm.RideHeightRear;
        part.AntiRollBarRear = vm.AntiRollBarRear;
        part.SpringRateRear = vm.SpringRateRear;
        part.DamperCompressionRear = vm.DamperCompressionRear;
        part.DamperExtensionRear = vm.DamperExtensionRear;
        part.CamberRear = vm.CamberRear;
        part.ToeRear = vm.ToeRear;

        part.LSDInitialFront = vm.LSDInitialFront;
        part.LSDAccelFront = vm.LSDAccelFront;
        part.LSDBrakingFront = vm.LSDBrakingFront;
        part.LSDInitialRear = vm.LSDInitialRear;
        part.LSDAccelRear = vm.LSDAccelRear;
        part.LSDBrakingRear = vm.LSDBrakingRear;
        part.TorqueDistribution = vm.TorqueDistribution;

        part.FinalGear = vm.FinalGear;
        part.TopSpeed = vm.TopSpeed;
        part.Gear1 = vm.Gear1;
        part.Gear2 = vm.Gear2;
        part.Gear3 = vm.Gear3;
        part.Gear4 = vm.Gear4;
        part.Gear5 = vm.Gear5;
        part.Gear6 = vm.Gear6;
        part.Gear7 = vm.Gear7;

        part.BrakeBalance = vm.BrakeBalance;
        part.FrontBrakePower = vm.FrontBrakePower;
        part.RearBrakePower = vm.RearBrakePower;

        part.TractionControl = vm.TractionControl;
        part.ABS = vm.ABS;
        part.ASM = vm.ASM;

        part.Turbocharger = vm.Turbocharger;
        part.Supercharger = vm.Supercharger;
        part.AntiLagSystem = vm.AntiLagSystem;
        part.Intercooler = vm.Intercooler;

        part.AirCleaner = vm.AirCleaner;
        part.ExhaustManifold = vm.ExhaustManifold;
        part.Muffler = vm.Muffler;
        part.CatalyticConverter = vm.CatalyticConverter;

        part.BrakeSystem = vm.BrakeSystem;
        part.BrakePads = vm.BrakePads;
        part.HandbrakeType = vm.HandbrakeType;
        part.HandbrakeTorque = vm.HandbrakeTorque;

        part.ChangeSteeringAngle = vm.ChangeSteeringAngle;
        part.FourWSSystem = vm.FourWSSystem;
        part.RearSteeringAngle = vm.RearSteeringAngle;

        part.ClutchAndFlywheel = vm.ClutchAndFlywheel;
        part.PropellerShaft = vm.PropellerShaft;

        part.BoreUp = vm.BoreUp;
        part.StrokeUp = vm.StrokeUp;
        part.EngineBalanceTuning = vm.EngineBalanceTuning;
        part.PolishPorts = vm.PolishPorts;
        part.HighLiftCamShaft = vm.HighLiftCamShaft;
        part.TitaniumConnectingRodsPistons = vm.TitaniumConnectingRodsPistons;
        part.RacingCrankShaft = vm.RacingCrankShaft;
        part.HighCompressionPistons = vm.HighCompressionPistons;

        part.WeightReductionStage1 = vm.WeightReductionStage1;
        part.WeightReductionStage2 = vm.WeightReductionStage2;
        part.WeightReductionStage3 = vm.WeightReductionStage3;
        part.WeightReductionStage4 = vm.WeightReductionStage4;
        part.WeightReductionStage5 = vm.WeightReductionStage5;
        part.IncreaseBodyRigidity = vm.IncreaseBodyRigidity;

        part.Description = vm.Description ?? string.Empty;
        part.RecommendedTrack = vm.RecommendedTrack ?? string.Empty;

        return Edit(part, context);
    }

    private async Task<List<CarOption>> LoadCarsAsync()
    {
        var cars = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "Car" && x.Latest && x.Published).ListAsync();

        return cars
            .Select(c => new CarOption(c.ContentItemId, string.IsNullOrWhiteSpace(c.DisplayText) ? c.ContentItemId : c.DisplayText))
            .OrderBy(o => o.DisplayText, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Bind(CarSetupPartViewModel model, CarSetupPart part)
    {
        model.CarContentItemId = part.CarContentItemId;

        model.PowerLevel = part.PowerLevel;
        model.WeightReduction = part.WeightReduction;
        model.Ballast = part.Ballast;
        model.BallastPosition = part.BallastPosition;
        model.PowerRestrictor = part.PowerRestrictor;

        model.FrontTires = part.FrontTires;
        model.RearTires = part.RearTires;

        model.FrontDownforce = part.FrontDownforce;
        model.RearDownforce = part.RearDownforce;

        model.RideHeightFront = part.RideHeightFront;
        model.AntiRollBarFront = part.AntiRollBarFront;
        model.SpringRateFront = part.SpringRateFront;
        model.DamperCompressionFront = part.DamperCompressionFront;
        model.DamperExtensionFront = part.DamperExtensionFront;
        model.CamberFront = part.CamberFront;
        model.ToeFront = part.ToeFront;

        model.RideHeightRear = part.RideHeightRear;
        model.AntiRollBarRear = part.AntiRollBarRear;
        model.SpringRateRear = part.SpringRateRear;
        model.DamperCompressionRear = part.DamperCompressionRear;
        model.DamperExtensionRear = part.DamperExtensionRear;
        model.CamberRear = part.CamberRear;
        model.ToeRear = part.ToeRear;

        model.LSDInitialFront = part.LSDInitialFront;
        model.LSDAccelFront = part.LSDAccelFront;
        model.LSDBrakingFront = part.LSDBrakingFront;
        model.LSDInitialRear = part.LSDInitialRear;
        model.LSDAccelRear = part.LSDAccelRear;
        model.LSDBrakingRear = part.LSDBrakingRear;
        model.TorqueDistribution = part.TorqueDistribution;

        model.FinalGear = part.FinalGear;
        model.TopSpeed = part.TopSpeed;
        model.Gear1 = part.Gear1;
        model.Gear2 = part.Gear2;
        model.Gear3 = part.Gear3;
        model.Gear4 = part.Gear4;
        model.Gear5 = part.Gear5;
        model.Gear6 = part.Gear6;
        model.Gear7 = part.Gear7;

        model.BrakeBalance = part.BrakeBalance;
        model.FrontBrakePower = part.FrontBrakePower;
        model.RearBrakePower = part.RearBrakePower;

        model.TractionControl = part.TractionControl;
        model.ABS = part.ABS;
        model.ASM = part.ASM;

        model.Turbocharger = part.Turbocharger;
        model.Supercharger = part.Supercharger;
        model.AntiLagSystem = part.AntiLagSystem;
        model.Intercooler = part.Intercooler;

        model.AirCleaner = part.AirCleaner;
        model.ExhaustManifold = part.ExhaustManifold;
        model.Muffler = part.Muffler;
        model.CatalyticConverter = part.CatalyticConverter;

        model.BrakeSystem = part.BrakeSystem;
        model.BrakePads = part.BrakePads;
        model.HandbrakeType = part.HandbrakeType;
        model.HandbrakeTorque = part.HandbrakeTorque;

        model.ChangeSteeringAngle = part.ChangeSteeringAngle;
        model.FourWSSystem = part.FourWSSystem;
        model.RearSteeringAngle = part.RearSteeringAngle;

        model.ClutchAndFlywheel = part.ClutchAndFlywheel;
        model.PropellerShaft = part.PropellerShaft;

        model.BoreUp = part.BoreUp;
        model.StrokeUp = part.StrokeUp;
        model.EngineBalanceTuning = part.EngineBalanceTuning;
        model.PolishPorts = part.PolishPorts;
        model.HighLiftCamShaft = part.HighLiftCamShaft;
        model.TitaniumConnectingRodsPistons = part.TitaniumConnectingRodsPistons;
        model.RacingCrankShaft = part.RacingCrankShaft;
        model.HighCompressionPistons = part.HighCompressionPistons;

        model.WeightReductionStage1 = part.WeightReductionStage1;
        model.WeightReductionStage2 = part.WeightReductionStage2;
        model.WeightReductionStage3 = part.WeightReductionStage3;
        model.WeightReductionStage4 = part.WeightReductionStage4;
        model.WeightReductionStage5 = part.WeightReductionStage5;
        model.IncreaseBodyRigidity = part.IncreaseBodyRigidity;

        model.Description = part.Description;
        model.RecommendedTrack = part.RecommendedTrack;
    }
}
