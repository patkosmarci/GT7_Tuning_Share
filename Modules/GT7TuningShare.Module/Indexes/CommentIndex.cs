using GT7TuningShare.Module.Models;
using YesSql.Indexes;

namespace GT7TuningShare.Module.Indexes;

public class CommentIndex : MapIndex
{
    public string UserId { get; set; } = string.Empty;
    public string SetupContentItemId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class CommentIndexProvider : IndexProvider<CommentRecord>
{
    public override void Describe(DescribeContext<CommentRecord> context)
    {
        context.For<CommentIndex>()
            .Map(record => new CommentIndex
            {
                UserId = record.UserId,
                SetupContentItemId = record.SetupContentItemId,
                CreatedUtc = record.CreatedUtc,
            });
    }
}
