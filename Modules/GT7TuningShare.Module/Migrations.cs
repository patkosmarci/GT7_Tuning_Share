using GT7TuningShare.Module.Indexes;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace GT7TuningShare.Module;

public sealed class Migrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public Migrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("CarPart", part => part
            .Attachable()
            .WithDescription("Catalog entry for a Gran Turismo 7 car."));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("Car", type => type
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart("TitlePart", p => p.WithPosition("0"))
            .WithPart("CarPart", p => p.WithPosition("1")));

        return 1;
    }

    public async Task<int> UpdateFrom1Async()
    {
        await _contentDefinitionManager.AlterPartDefinitionAsync("CarSetupPart", part => part
            .Attachable()
            .WithDescription("Tunable parameters of a Gran Turismo 7 car setup."));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("CarSetup", type => type
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .Securable()
            .WithPart("TitlePart", p => p.WithPosition("0"))
            .WithPart("CarSetupPart", p => p.WithPosition("1")));

        return 2;
    }

    public async Task<int> UpdateFrom2Async()
    {
        await SchemaBuilder.CreateMapIndexTableAsync<RatingIndex>(table => table
            .Column<string>("UserId", c => c.WithLength(255))
            .Column<string>("SetupContentItemId", c => c.WithLength(26))
            .Column<int>("Stars")
            .Column<DateTime>("CreatedUtc"));

        await SchemaBuilder.AlterIndexTableAsync<RatingIndex>(table => table
            .CreateIndex("IDX_RatingIndex_Setup", "SetupContentItemId"));

        await _contentDefinitionManager.AlterPartDefinitionAsync("RatingPart", part => part
            .Attachable()
            .WithDescription("Aggregate rating cache (average + count) for a CarSetup."));

        await _contentDefinitionManager.AlterTypeDefinitionAsync("CarSetup", type => type
            .WithPart("RatingPart", p => p.WithPosition("2")));

        return 3;
    }

    public async Task<int> UpdateFrom3Async()
    {
        await SchemaBuilder.CreateMapIndexTableAsync<CommentIndex>(table => table
            .Column<string>("UserId", c => c.WithLength(255))
            .Column<string>("SetupContentItemId", c => c.WithLength(26))
            .Column<DateTime>("CreatedUtc"));

        await SchemaBuilder.AlterIndexTableAsync<CommentIndex>(table => table
            .CreateIndex("IDX_CommentIndex_Setup", "SetupContentItemId"));

        return 4;
    }
}
