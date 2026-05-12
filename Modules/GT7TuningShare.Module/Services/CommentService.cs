using GT7TuningShare.Module.Indexes;
using GT7TuningShare.Module.Models;
using YesSql;

namespace GT7TuningShare.Module.Services;

public sealed class CommentService : ICommentService
{
    private const int MaxBodyLength = 2000;
    private readonly ISession _session;

    public CommentService(ISession session)
    {
        _session = session;
    }

    public async Task<List<CommentRecord>> ListAsync(string setupContentItemId)
    {
        if (string.IsNullOrEmpty(setupContentItemId)) return new List<CommentRecord>();

        var comments = await _session.Query<CommentRecord, CommentIndex>(
            x => x.SetupContentItemId == setupContentItemId)
            .OrderByDescending(x => x.CreatedUtc)
            .ListAsync();

        return comments.ToList();
    }

    public async Task<CommentRecord> AddAsync(string userName, string setupContentItemId, string body)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrEmpty(setupContentItemId)) throw new ArgumentException("Setup id is required.", nameof(setupContentItemId));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Comment body cannot be empty.", nameof(body));

        body = body.Trim();
        if (body.Length > MaxBodyLength)
        {
            body = body.Substring(0, MaxBodyLength);
        }

        var record = new CommentRecord
        {
            UserId = userName,
            SetupContentItemId = setupContentItemId,
            Body = body,
            CreatedUtc = DateTime.UtcNow,
        };

        await _session.SaveAsync(record);
        await _session.SaveChangesAsync();

        return record;
    }

    public async Task<int> RemoveAllByUserAsync(string userName, string setupContentItemId)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(setupContentItemId)) return 0;

        var records = await _session.Query<CommentRecord, CommentIndex>(
            x => x.UserId == userName && x.SetupContentItemId == setupContentItemId)
            .ListAsync();

        var count = 0;
        foreach (var r in records)
        {
            _session.Delete(r);
            count++;
        }

        if (count > 0)
        {
            await _session.SaveChangesAsync();
        }
        return count;
    }

    public async Task<bool> RemoveByIdAsync(long commentId, string userName)
    {
        if (string.IsNullOrEmpty(userName) || commentId <= 0) return false;

        var records = await _session.GetAsync<CommentRecord>(new[] { commentId });
        var record = records.FirstOrDefault();
        if (record is null) return false;
        if (!string.Equals(record.UserId, userName, StringComparison.Ordinal)) return false;

        _session.Delete(record);
        await _session.SaveChangesAsync();
        return true;
    }
}
