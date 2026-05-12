using GT7TuningShare.Module.Models;
using YesSql.Indexes;

namespace GT7TuningShare.Module.Indexes;

public class RatingIndex : MapIndex
{
    public string UserId { get; set; } = string.Empty;
    public string SetupContentItemId { get; set; } = string.Empty;
    public int Stars { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class RatingIndexProvider : IndexProvider<RatingRecord>
{
    public override void Describe(DescribeContext<RatingRecord> context)
    {
        context.For<RatingIndex>()
            .Map(record => new RatingIndex
            {
                UserId = record.UserId,
                SetupContentItemId = record.SetupContentItemId,
                Stars = record.Stars,
                CreatedUtc = record.CreatedUtc,
            });
    }
}
