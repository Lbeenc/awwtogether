using System.Text.Json;
using AwwTogether.Api.Models;

namespace AwwTogether.Api.Services;

/// <summary>
/// Reads site copy from Data/content.json and reloads it when the file changes,
/// so text and prices can be updated without a redeploy.
/// </summary>
public sealed class ContentStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _path;
    private readonly ILogger<ContentStore> _logger;
    private readonly FileSystemWatcher? _watcher;
    private SiteContent _current;

    public ContentStore(IWebHostEnvironment env, ILogger<ContentStore> logger)
    {
        _logger = logger;
        _path = Path.Combine(env.ContentRootPath, "Data", "content.json");
        _current = Load() ?? throw new InvalidOperationException($"Could not load site content from {_path}");

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_path)!, Path.GetFileName(_path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += (_, _) => Reload();
    }

    public SiteContent Current => _current;

    private void Reload()
    {
        // Editors often write in several chunks; give them a moment.
        Thread.Sleep(150);
        if (Load() is { } fresh)
        {
            _current = fresh;
            _logger.LogInformation("Reloaded site content");
        }
    }

    private SiteContent? Load()
    {
        try
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return JsonSerializer.Deserialize<SiteContent>(stream, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Failed to read {Path}; keeping the previous content", _path);
            return null;
        }
    }

    public void Dispose() => _watcher?.Dispose();
}
