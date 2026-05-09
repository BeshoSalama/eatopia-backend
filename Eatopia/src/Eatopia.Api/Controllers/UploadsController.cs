using Eatopia.Api.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eatopia.Api.Controllers;

[Route("api/uploads")]
[Route("api/v1/uploads")]
[ApiController]
[Authorize]
public class UploadsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp4", ".webm", ".mov",
        ".mp3", ".wav", ".m4a", ".ogg",
        ".pdf", ".doc", ".docx", ".txt"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".mov" };
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".m4a", ".ogg", ".webm" };

    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(IWebHostEnvironment environment, IConfiguration configuration, ILogger<UploadsController> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? purpose = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file uploaded." });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return BadRequest(new { success = false, message = "Unsupported file type." });

        var maxBytes = ImageExtensions.Contains(extension) ? 5 * 1024 * 1024
            : VideoExtensions.Contains(extension) ? 25 * 1024 * 1024
            : AudioExtensions.Contains(extension) ? 10 * 1024 * 1024
            : 8 * 1024 * 1024;

        if (file.Length > maxBytes)
            return BadRequest(new { success = false, message = $"Maximum size for this file type is {maxBytes / 1024 / 1024} MB." });

        if (!IsContentTypeAllowed(extension, file.ContentType))
            return BadRequest(new { success = false, message = "File content type does not match the allowed type." });

        var safePurpose = NormalizePurpose(purpose);
        var relativeFolder = Path.Combine(safePurpose, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        var physicalFolder = Path.Combine(UploadStorageHelper.GetPersistentUploadsRoot(_configuration), relativeFolder);
        Directory.CreateDirectory(physicalFolder);

        var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(physicalFolder, safeFileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = UploadStorageHelper.ToRelativeUploadUrl(Path.Combine(relativeFolder, safeFileName));
        var absoluteUrl = BuildAbsoluteUploadUrl(relativeUrl);

        _logger.LogInformation("Uploaded file {FileName} as {Url}", file.FileName, relativeUrl);

        return Ok(new
        {
            success = true,
            // Browser clients can render this directly from Vercel.
            url = absoluteUrl,
            absoluteUrl,
            // Store this value in the database. It stays valid if the API port/domain changes.
            relativeUrl,
            storedUrl = relativeUrl,
            fileName = file.FileName,
            size = file.Length,
            contentType = file.ContentType
        });
    }

    [HttpGet("check")]
    public IActionResult Check([FromQuery] string? path = null, [FromQuery] string? url = null)
    {
        var raw = string.IsNullOrWhiteSpace(path) ? url : path;
        var relative = ExtractUploadRelativePath(raw);
        var roots = UploadStorageHelper.GetUploadRoots(_environment, _configuration);
        var checkedPaths = new List<object>();
        var exists = false;

        foreach (var root in roots)
        {
            var fullPath = string.IsNullOrWhiteSpace(relative) ? root : Path.Combine(root, relative);
            var fileExists = System.IO.File.Exists(fullPath);
            checkedPaths.Add(new { root, fullPath, exists = fileExists });
            exists |= fileExists;
        }

        return Ok(new
        {
            success = true,
            exists,
            requested = raw,
            relativePath = relative,
            uploadRoots = roots,
            checkedPaths
        });
    }

    private static string ExtractUploadRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().Replace("\\", "/");
        var marker = "/uploads/";
        var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0) return normalized[(index + marker.Length)..].TrimStart('/');
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)) return normalized["uploads/".Length..].TrimStart('/');
        return normalized.TrimStart('/');
    }

    private static string NormalizePurpose(string? purpose)
    {
        var value = (purpose ?? "general").Trim().ToLowerInvariant();
        return value switch
        {
            "profile" or "profiles" => "profiles",
            "recipe" or "recipes" => "recipes",
            "post" or "posts" or "community" => "posts",
            "chat" or "message" or "messages" => "chat",
            "audio" or "voice" or "chat-audio" => "chat-audio",
            "video" => "chat-video",
            "file" or "files" => "files",
            _ => "general"
        };
    }

    private static bool IsContentTypeAllowed(string extension, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        contentType = contentType.ToLowerInvariant();

        if (ImageExtensions.Contains(extension)) return contentType.StartsWith("image/");
        if (extension.Equals(".webm", StringComparison.OrdinalIgnoreCase)) return contentType.StartsWith("audio/") || contentType.StartsWith("video/");
        if (VideoExtensions.Contains(extension)) return contentType.StartsWith("video/");
        if (AudioExtensions.Contains(extension)) return contentType.StartsWith("audio/");
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) return contentType == "application/pdf";
        if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) return contentType.StartsWith("text/");
        if (extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) || extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Contains("word") || contentType.Contains("officedocument");
        }

        return false;
    }

    private string BuildAbsoluteUploadUrl(string relativeUrl)
    {
        var configuredBaseUrl = (_configuration["Api:BaseUrl"] ?? Environment.GetEnvironmentVariable("API_BASE_URL"))?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            if (configuredBaseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                configuredBaseUrl = configuredBaseUrl[..^4];

            return $"{configuredBaseUrl}{relativeUrl}";
        }

        return $"{Request.Scheme}://{Request.Host}{relativeUrl}";
    }
}
