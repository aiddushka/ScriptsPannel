using System.IO;
using System.Text.Json;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
    string GetEffectiveDataRoot();
}

public sealed class SettingsService : ISettingsService
{
    private readonly string _path;

    public SettingsService(string? path = null)
    {
        _path = path ?? AppPaths.DefaultSettingsPath;
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Instance) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions.Instance));
    }

    public string GetEffectiveDataRoot()
    {
        var s = Load();
        if (!string.IsNullOrWhiteSpace(s.ScriptsDataRoot))
        {
            try
            {
                var full = Path.GetFullPath(s.ScriptsDataRoot);
                return full;
            }
            catch
            {
                /* fallthrough */
            }
        }
        return AppPaths.DefaultDataRoot;
    }
}
