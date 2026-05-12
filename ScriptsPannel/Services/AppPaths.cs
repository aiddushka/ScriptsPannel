using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public sealed class AppPaths
{
    public static string LocalAppFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScriptsPannel");

    public static string DefaultSettingsPath => Path.Combine(LocalAppFolder, "settings.json");

    public static string ExecutableDirectory =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

    public static string DefaultDataRoot =>
        Path.Combine(ExecutableDirectory, "Data");

    public static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var s = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(s) ? "shelf" : s;
    }
}

public sealed class JsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
