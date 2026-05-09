using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Eatopia.Application.DTOs.AI;
using Eatopia.Application.Exceptions;
using Eatopia.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eatopia.Infrastructure.AI;

public class PythonAiClient : IFoodAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PythonAiClient> _logger;

    public PythonAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<PythonAiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (HasConfiguredHttpService())
        {
            var json = await PostJsonToAiServiceAsync(
                "scan-food",
                new { imagePath = imageUrl },
                "food scan",
                TimeSpan.FromSeconds(150),
                cancellationToken);

            return DeserializeAiFoodResult(json);
        }

        var localJson = await RunPythonAsync("scan", new { imagePath = imageUrl }, TimeSpan.FromSeconds(150), cancellationToken);
        return DeserializeAiFoodResult(localJson);
    }

    public async Task<AiFoodResultDto> AnalyzeFoodImageAsync(
        Stream imageStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (HasConfiguredHttpService())
        {
            var json = await PostImageToAiServiceAsync(imageStream, fileName, contentType, cancellationToken);
            return DeserializeAiFoodResult(json);
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var tempFolder = Path.Combine(Path.GetTempPath(), "eatopia-ai-scans");
        Directory.CreateDirectory(tempFolder);
        var tempPath = Path.Combine(tempFolder, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        await using (var file = File.Create(tempPath))
        {
            await imageStream.CopyToAsync(file, cancellationToken);
        }

        try
        {
            return await AnalyzeFoodImageAsync(tempPath, cancellationToken);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public async Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(
        GenerateFrontendDietPlanRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (HasConfiguredHttpService())
        {
            var payload = new
            {
                age = dto.Age,
                weight = dto.WeightKg,
                height = dto.HeightCm,
                activity = dto.ActivityLevel,
                goal = dto.Goal,
                durationDays = dto.DurationDays
            };

            var json = await PostJsonToAiServiceAsync(
                "diet-plan",
                payload,
                "diet plan",
                TimeSpan.FromSeconds(45),
                cancellationToken);

            return DeserializePayload<FrontendDietPlanResponseDto>(json, "AI diet plan response was empty.");
        }

        var localJson = await RunPythonAsync("diet-plan", dto, TimeSpan.FromSeconds(45), cancellationToken);
        return DeserializePayload<FrontendDietPlanResponseDto>(localJson, "AI diet plan response was empty.");
    }

    private bool HasConfiguredHttpService() => !string.IsNullOrWhiteSpace(_configuration["AI:ServiceUrl"]);

    private async Task<string> PostImageToAiServiceAsync(
        Stream imageStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var endpoint = ResolveServiceEndpoint("scan-food");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);

        streamContent.Headers.ContentType = TryParseMediaType(contentType)
            ?? new MediaTypeHeaderValue("application/octet-stream");

        var safeFileName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "scan.jpg" : fileName);
        form.Add(streamContent, "image", safeFileName);
        request.Content = form;

        return await SendAiRequestAsync(request, "food scan", TimeSpan.FromSeconds(150), cancellationToken);
    }

    private async Task<string> PostJsonToAiServiceAsync(
        string endpointName,
        object payload,
        string operationName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var endpoint = ResolveServiceEndpoint(endpointName);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        return await SendAiRequestAsync(request, operationName, timeout, cancellationToken);
    }

    private async Task<string> SendAiRequestAsync(
        HttpRequestMessage request,
        string operationName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            var body = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
                throw BuildAiServiceException(operationName, response.StatusCode, body);

            if (string.IsNullOrWhiteSpace(body))
                throw new ApiException($"AI service returned an empty {operationName} response.", 502, "AI_EMPTY_RESPONSE");

            return body;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ApiException($"AI service {operationName} timed out after {timeout.TotalSeconds:0} seconds.", 504, "AI_TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI service {OperationName} request failed.", operationName);
            throw new ApiException($"AI service {operationName} request failed: {ex.Message}", 502, "AI_SERVICE_UNAVAILABLE");
        }
    }

    private Uri ResolveServiceEndpoint(string endpointName)
    {
        var configured = _configuration["AI:ServiceUrl"]?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(configured))
            throw new ApiException("AI service URL is not configured. Set AI_SERVICE_URL or AI__ServiceUrl.", 503, "AI_NOT_CONFIGURED");

        if (!Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri))
            throw new ApiException("AI service URL must be an absolute HTTP or HTTPS URL.", 503, "AI_INVALID_URL");

        if (configuredUri.Scheme != Uri.UriSchemeHttp && configuredUri.Scheme != Uri.UriSchemeHttps)
            throw new ApiException("AI service URL must use HTTP or HTTPS.", 503, "AI_INVALID_URL");

        if (configured.EndsWith($"/{endpointName}", StringComparison.OrdinalIgnoreCase))
            return configuredUri;

        if (configured.EndsWith("/scan-food", StringComparison.OrdinalIgnoreCase) ||
            configured.EndsWith("/diet-plan", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(configuredUri, $"/{endpointName}");
        }

        return new Uri($"{configured}/{endpointName}");
    }

    private static ApiException BuildAiServiceException(string operationName, HttpStatusCode statusCode, string body)
    {
        var message = ExtractAiErrorMessage(body);
        var frontendStatus = statusCode is HttpStatusCode.BadRequest
            or HttpStatusCode.UnsupportedMediaType
            or HttpStatusCode.RequestEntityTooLarge
            or HttpStatusCode.UnprocessableEntity
                ? (int)statusCode
                : 502;

        return new ApiException(
            $"AI service {operationName} failed ({(int)statusCode}): {message}",
            frontendStatus,
            "AI_SERVICE_ERROR");
    }

    private static AiFoodResultDto DeserializeAiFoodResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        var payload = SelectPayload(document.RootElement);

        var result = payload.Deserialize<AiFoodResultDto>(JsonOptions)
            ?? throw new ApiException("AI scan response was empty.", 502, "AI_EMPTY_RESPONSE");

        if (string.IsNullOrWhiteSpace(result.FoodName))
        {
            result.FoodName = ReadString(payload, "foodName", "food_name", "name", "title") ?? "Scanned Meal";
        }

        result.Source ??= ReadString(payload, "source");
        result.Note ??= ReadString(payload, "note");
        result.Message ??= ReadString(payload, "message");
        result.ModelError ??= ReadString(payload, "modelError", "model_error");

        return result;
    }

    private static T DeserializePayload<T>(string json, string emptyMessage)
    {
        using var document = JsonDocument.Parse(json);
        var payload = SelectPayload(document.RootElement);

        return payload.Deserialize<T>(JsonOptions)
            ?? throw new ApiException(emptyMessage, 502, "AI_EMPTY_RESPONSE");
    }

    private static JsonElement SelectPayload(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return root;

        if (root.TryGetProperty("success", out var success) &&
            success.ValueKind is JsonValueKind.False &&
            !success.GetBoolean())
        {
            throw new ApiException(ExtractAiErrorMessage(root.GetRawText()), 502, "AI_SERVICE_ERROR");
        }

        foreach (var propertyName in new[] { "result", "data" })
        {
            if (root.TryGetProperty(propertyName, out var property) &&
                property.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                return property;
            }
        }

        return root;
    }

    private static string? ReadString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
                return property.GetString();
        }

        return null;
    }

    private static string ExtractAiErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "No response body was returned.";

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                var message = ReadString(root, "message", "error", "title");
                if (!string.IsNullOrWhiteSpace(message))
                    return message;

                if (root.TryGetProperty("error", out var errorObject) && errorObject.ValueKind == JsonValueKind.Object)
                {
                    message = ReadString(errorObject, "message", "detail", "title");
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }

                if (root.TryGetProperty("detail", out var detail))
                {
                    if (detail.ValueKind == JsonValueKind.String)
                        return detail.GetString() ?? "Validation failed.";

                    if (detail.ValueKind == JsonValueKind.Array)
                    {
                        var errors = detail.EnumerateArray()
                            .Select(item =>
                            {
                                if (item.ValueKind != JsonValueKind.Object)
                                    return item.ToString();

                                var location = item.TryGetProperty("loc", out var loc) ? loc.ToString() : null;
                                var msg = ReadString(item, "msg", "message") ?? item.ToString();
                                return string.IsNullOrWhiteSpace(location) ? msg : $"{location}: {msg}";
                            })
                            .Where(item => !string.IsNullOrWhiteSpace(item))
                            .ToArray();

                        if (errors.Length > 0)
                            return string.Join("; ", errors);
                    }
                }
            }
        }
        catch
        {
            // Fall back to a trimmed response body below.
        }

        var trimmed = body.Trim();
        return trimmed.Length <= 1000 ? trimmed : $"{trimmed[..1000]}...";
    }

    private async Task<string> RunPythonAsync(string command, object payload, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var aiRoot = ResolveAiRoot();
        var scriptPath = Path.Combine(aiRoot, "eatopia_ai_cli.py");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Eatopia AI bridge script was not found.", scriptPath);

        var pythonExecutable = ResolvePythonExecutable(aiRoot);
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            WorkingDirectory = aiRoot,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(command);
        AddVenvSitePackagesToPythonPath(startInfo, aiRoot);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new ApiException("Failed to start Python AI process.", 502, "AI_PROCESS_FAILED");

        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new ApiException($"Python AI command '{command}' timed out after {timeout.TotalSeconds:0} seconds.", 504, "AI_TIMEOUT");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(error))
            _logger.LogWarning("Python AI stderr for {Command}: {Error}", command, error.Trim());

        if (process.ExitCode != 0)
            throw new ApiException($"Python AI command '{command}' failed: {error}", 502, "AI_PROCESS_FAILED");

        var json = ExtractJson(output);
        if (string.IsNullOrWhiteSpace(json))
            throw new ApiException($"Python AI command '{command}' returned no JSON.", 502, "AI_EMPTY_RESPONSE");

        return json;
    }

    private string ResolveAiRoot()
    {
        var configured = _configuration["AI:RootPath"];
        var candidates = new[]
        {
            configured,
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "ai"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "ai")
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate));
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "eatopia_ai_cli.py")))
                return fullPath;
        }

        throw new DirectoryNotFoundException("Could not find the Eatopia ai folder. Configure AI:RootPath in appsettings.json.");
    }

    private string ResolvePythonExecutable(string aiRoot)
    {
        var configured = _configuration["AI:PythonExecutable"];
        var venvPython = Path.Combine(aiRoot, ".venv", "Scripts", "python.exe");
        var profileCandidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetEnvironmentVariable("USERPROFILE"),
            ResolveUserProfileFromLocalAppData()
        };

        var candidates = new List<string?>
        {
            configured,
            venvPython,
        };

        foreach (var profile in profileCandidates)
        {
            if (!string.IsNullOrWhiteSpace(profile))
            {
                candidates.Add(
                    Path.Combine(
                        profile,
                        ".cache",
                        "codex-runtimes",
                        "codex-primary-runtime",
                        "dependencies",
                        "python",
                        "python.exe"));
            }
        }

        candidates.AddRange(new[]
        {
            "python",
            "py"
        });

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var executable = Environment.ExpandEnvironmentVariables(candidate);
            if (!seen.Add(executable))
                continue;

            if (IsUsablePython(executable))
                return executable;
        }

        throw new FileNotFoundException("Could not find a usable Python executable. Configure AI:PythonExecutable in appsettings.json.");
    }

    private static void AddVenvSitePackagesToPythonPath(ProcessStartInfo startInfo, string aiRoot)
    {
        var sitePackages = Path.Combine(aiRoot, ".venv", "Lib", "site-packages");
        if (!Directory.Exists(sitePackages))
            return;

        startInfo.Environment.TryGetValue("PYTHONPATH", out var existingPythonPath);
        startInfo.Environment["PYTHONPATH"] = string.IsNullOrWhiteSpace(existingPythonPath)
            ? sitePackages
            : $"{sitePackages}{Path.PathSeparator}{existingPythonPath}";
    }

    private static string? ResolveUserProfileFromLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            return null;

        var localDirectory = Directory.GetParent(localAppData);
        return localDirectory?.Parent?.FullName;
    }

    private static bool IsUsablePython(string executable)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--version");

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            return process.WaitForExit(3000) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static MediaTypeHeaderValue? TryParseMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        return MediaTypeHeaderValue.TryParse(contentType, out var mediaType) ? mediaType : null;
    }

    private static string ExtractJson(string output)
    {
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("{") || line.StartsWith("["))
                return line;
        }

        return output.Trim();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort cleanup after temporary image scans.
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup after a timeout.
        }
    }
}
