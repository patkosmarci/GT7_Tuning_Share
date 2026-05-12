namespace GT7TuningShare.Module.ViewModels;

public class CarSetupPartViewModel
{
    [System.ComponentModel.DataAnnotations.Display(Name = "Car")]
    public string? CarContentItemId { get; set; }

    public string? EngineSwap { get; set; }

    public double PowerLevel { get; set; } = 100;
    public double WeightReduction { get; set; }
    public int Ballast { get; set; }
    public int BallastPosition { get; set; }
    public int PowerRestrictor { get; set; } = 100;

    public string? FrontTires { get; set; }
    public string? RearTires { get; set; }

    public int FrontDownforce { get; set; }
    public int RearDownforce { get; set; }

    public int RideHeightFront { get; set; }
    public int AntiRollBarFront { get; set; } = 5;
    public double SpringRateFront { get; set; }
    public int DamperCompressionFront { get; set; } = 5;
    public int DamperExtensionFront { get; set; } = 5;
    public double CamberFront { get; set; }
    public double ToeFront { get; set; }

    public int RideHeightRear { get; set; }
    public int AntiRollBarRear { get; set; } = 5;
    public double SpringRateRear { get; set; }
    public int DamperCompressionRear { get; set; } = 5;
    public int DamperExtensionRear { get; set; } = 5;
    public double CamberRear { get; set; }
    public double ToeRear { get; set; }

    public int LSDInitialFront { get; set; } = 10;
    public int LSDAccelFront { get; set; } = 10;
    public int LSDBrakingFront { get; set; } = 10;
    public int LSDInitialRear { get; set; } = 10;
    public int LSDAccelRear { get; set; } = 10;
    public int LSDBrakingRear { get; set; } = 10;
    public int TorqueDistribution { get; set; } = 50;

    public double FinalGear { get; set; }
    public int TopSpeed { get; set; }
    public double Gear1 { get; set; }
    public double Gear2 { get; set; }
    public double Gear3 { get; set; }
    public double Gear4 { get; set; }
    public double Gear5 { get; set; }
    public double Gear6 { get; set; }
    public double Gear7 { get; set; }

    public int BrakeBalance { get; set; }
    public int FrontBrakePower { get; set; } = 5;
    public int RearBrakePower { get; set; } = 5;

    public int TractionControl { get; set; }
    public int ABS { get; set; } = 1;
    public int ASM { get; set; }

    public string? Turbocharger { get; set; }
    public string? Supercharger { get; set; }
    public bool AntiLagSystem { get; set; }
    public string? Intercooler { get; set; }

    public string? AirCleaner { get; set; }
    public string? ExhaustManifold { get; set; }
    public string? Muffler { get; set; }
    public string? CatalyticConverter { get; set; }

    public string? BrakeSystem { get; set; }
    public string? BrakePads { get; set; }
    public string? HandbrakeType { get; set; }
    public int HandbrakeTorque { get; set; }

    public bool ChangeSteeringAngle { get; set; }
    public bool FourWSSystem { get; set; }
    public double RearSteeringAngle { get; set; }

    public string? ClutchAndFlywheel { get; set; }
    public string? PropellerShaft { get; set; }

    public bool BoreUp { get; set; }
    public bool StrokeUp { get; set; }
    public bool EngineBalanceTuning { get; set; }
    public bool PolishPorts { get; set; }
    public bool HighLiftCamShaft { get; set; }
    public bool TitaniumConnectingRodsPistons { get; set; }
    public bool RacingCrankShaft { get; set; }
    public bool HighCompressionPistons { get; set; }

    public bool WeightReductionStage1 { get; set; }
    public bool WeightReductionStage2 { get; set; }
    public bool WeightReductionStage3 { get; set; }
    public bool WeightReductionStage4 { get; set; }
    public bool WeightReductionStage5 { get; set; }
    public bool IncreaseBodyRigidity { get; set; }

    public string? Description { get; set; }
    public string? RecommendedTrack { get; set; }

    // Populated by the driver — list of (id, displayText) for the car selector.
    public List<CarOption> AvailableCars { get; set; } = new();
}

public record CarOption(string ContentItemId, string DisplayText, int GameId = 0);
