using GT7TuningShare.Module.Services;
using GT7TuningShare.Module.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GT7TuningShare.Module.Api;

[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("setups/rate")]
public class RatingsApiController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsApiController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RatingApiModel model)
    {
        if (model is null) return BadRequest(new { error = "Missing body." });
        if (model.Stars < 1 || model.Stars > 5) return BadRequest(new { error = "Stars must be between 1 and 5." });
        if (string.IsNullOrEmpty(model.SetupContentItemId)) return BadRequest(new { error = "Missing setup id." });

        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var result = await _ratingService.UpsertAsync(userName, model.SetupContentItemId, model.Stars);

        return Ok(new
        {
            average = Math.Round(result.Average, 2),
            count = result.Count,
            myRating = result.MyRating,
        });
    }
}
