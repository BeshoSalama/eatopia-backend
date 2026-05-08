using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Eatopia.Api.Common;

public static class UploadStorageHelper
{
    public const string UploadsRequestPath = "/uploads";

    public static string GetPersistentUploadsRoot(IConfiguration configuration)
    {
        var configured = configuration["MediaStorage:UploadsRoot"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = Environment.GetEnvironmentVariable("EATOPIA_UPLOADS_ROOT");
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configured = string.IsNullOrWhiteSpace(localAppData)
                ? Path.Combine(AppContext.BaseDirectory, "EatopiaUploads")
                : Path.Combine(localAppData, "Eatopia", "uploads");
        }

        configured = ExpandPath(configured);
        Directory.CreateDirectory(configured);
        return Path.GetFullPath(configured);
    }

    public static IReadOnlyList<string> GetUploadRoots(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var roots = new List<string> { GetPersistentUploadsRoot(configuration) };

        var contentRoot = environment.ContentRootPath;
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(contentRoot, "wwwroot")
            : environment.WebRootPath;

        roots.Add(Path.Combine(webRoot, "uploads"));
        roots.Add(Path.Combine(contentRoot, "wwwroot", "uploads"));
        roots.Add(Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads"));

        var additionalRoots = configuration.GetSection("MediaStorage:AdditionalUploadRoots").GetChildren().Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x));
        foreach (var root in additionalRoots)
        {
            if (!string.IsNullOrWhiteSpace(root)) roots.Add(ExpandPath(root));
        }

        return roots
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Path.GetFullPath(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    public static IFileProvider BuildCompositeUploadFileProvider(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var roots = GetUploadRoots(environment, configuration);
        foreach (var root in roots) Directory.CreateDirectory(root);
        TrySeedPersistentStore(roots[0], roots.Skip(1));
        return new CompositeFileProvider(roots.Select(root => new PhysicalFileProvider(root)).ToArray());
    }

    public static string ToRelativeUploadUrl(string relativePath)
    {
        var safe = relativePath.Replace("\\", "/").TrimStart('/');
        return $"{UploadsRequestPath}/{safe}";
    }

    private static string ExpandPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        if (expanded.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = Path.Combine(home, expanded.TrimStart('~', '/', '\\'));
        }
        return expanded;
    }

    private static void TrySeedPersistentStore(string persistentRoot, IEnumerable<string> candidateRoots)
    {
        foreach (var root in candidateRoots)
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                CopyDirectory(root, persistentRoot);
            }
            catch
            {
                // Best-effort only. The app can still serve from this root directly.
            }
        }
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, file);
            var destination = Path.Combine(destinationRoot, relative);
            if (File.Exists(destination)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: false);
        }
    }
}
