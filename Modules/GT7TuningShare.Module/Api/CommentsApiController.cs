using GT7TuningShare.Module.Services;
using GT7TuningShare.Module.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GT7TuningShare.Module.Api;

[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("setups/comment")]
public class CommentsApiController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsApiController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CommentApiModel model)
    {
        if (model is null) return BadRequest(new { error = "Missing body." });
        if (string.IsNullOrEmpty(model.SetupContentItemId)) return BadRequest(new { error = "Missing setup id." });
        if (string.IsNullOrWhiteSpace(model.Body)) return BadRequest(new { error = "Comment cannot be empty." });

        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var record = await _commentService.AddAsync(userName, model.SetupContentItemId, model.Body);

        return Ok(new
        {
            id = record.Id,
            userName = record.UserId,
            body = record.Body,
            createdUtc = record.CreatedUtc.ToString("O"),
            createdDisplay = record.CreatedUtc.ToString("yyyy-MM-dd HH:mm"),
        });
    }
}
