using System.Diagnostics;
using System.Text.Json;
using Eatopia.Application.DTOs.AI;
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

    private readonly IConfiguration _configuration;
    private readonly ILogger<PythonAiClient> _logger;

    public PythonAiClient(IConfiguration configuration, ILogger<PythonAiClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiFoodResultDto> AnalyzeFoodImageAsync(string imageUrl)
    {
        var json = await RunPythonAsync("scan", new { imagePath = imageUrl }, TimeSpan.FromSeconds(150));
        return JsonSerializer.Deserialize<AiFoodResultDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("AI scan response was empty.");
    }

    public async Task<FrontendDietPlanResponseDto> GenerateDietPlanAsync(GenerateFrontendDietPlanRequestDto dto)
    {
        var json = await RunPythonAsync("diet-plan", dto, TimeSpan.FromSeconds(45));
        return JsonSerializer.Deserialize<FrontendDietPlanResponseDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("AI diet plan response was empty.");
    }

    private async Task<string> RunPythonAsync(string command, object payload, TimeSpan timeout)
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
            throw new InvalidOperationException("Failed to start Python AI process.");

        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw new TimeoutException($"Python AI command '{command}' timed out after {timeout.TotalSeconds:0} seconds.");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(error))
            _logger.LogWarning("Python AI stderr for {Command}: {Error}", command, error.Trim());

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Python AI command '{command}' failed: {error}");

        var json = ExtractJson(output);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Python AI command '{command}' returned no JSON.");

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
