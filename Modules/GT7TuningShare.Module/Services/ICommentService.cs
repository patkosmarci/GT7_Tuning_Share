using GT7TuningShare.Module.Models;

namespace GT7TuningShare.Module.Services;

public interface ICommentService
{
    Task<List<CommentRecord>> ListAsync(string setupContentItemId);
    Task<CommentRecord> AddAsync(string userName, string setupContentItemId, string body);
    Task<int> RemoveAllByUserAsync(string userName, string setupContentItemId);
    /// Deletes a single comment by id, only if it belongs to <paramref name="userName"/>.
    Task<bool> RemoveByIdAsync(long commentId, string userName);
}
