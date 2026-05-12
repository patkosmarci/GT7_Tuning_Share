using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GT7TuningShare.Module.Api;

[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("setups/comment-image")]
public class CommentImageApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public CommentImageApiController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Post(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded." });
        }
        if (file.Length > MaxBytes)
        {
            return BadRequest(new { error = "Image too large (max 5MB)." });
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            ext = file.ContentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ""
            };
        }
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            return BadRequest(new { error = "Unsupported image type." });
        }

        var name = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "Sites", "Default", "Media", "comments");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);

        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { url = $"/media/comments/{name}" });
    }
}
