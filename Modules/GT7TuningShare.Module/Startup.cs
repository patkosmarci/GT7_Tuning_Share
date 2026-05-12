using GT7TuningShare.Module.Drivers;
using GT7TuningShare.Module.Indexes;
using GT7TuningShare.Module.Models;
using GT7TuningShare.Module.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using YesSql.Indexes;

namespace GT7TuningShare.Module;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddContentPart<CarPart>()
            .UseDisplayDriver<CarPartDisplayDriver>();

        services.AddContentPart<CarSetupPart>()
            .UseDisplayDriver<CarSetupPartDisplayDriver>();

        services.AddContentPart<RatingPart>();

        services.AddDataMigration<Migrations>();
        services.AddScoped<IModularTenantEvents, CarsSeeder>();

        services.AddIndexProvider<RatingIndexProvider>();
        services.AddScoped<IRatingService, RatingService>();

        services.AddIndexProvider<CommentIndexProvider>();
        services.AddScoped<ICommentService, CommentService>();

        services.AddSingleton<IEngineSwapCatalog, EngineSwapCatalog>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "Home",
            areaName: "GT7TuningShare.Module",
            pattern: "Home/Index",
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}

