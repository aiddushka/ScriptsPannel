using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public interface IScriptDataStore
{
    string DataRoot { get; }
    DataIndex LoadIndex();
    void SaveIndex(DataIndex index);
    string ShelfDirectory(Guid shelfId);
    List<ScriptEntry> LoadScripts(Guid shelfId);
    void SaveScript(Guid shelfId, ScriptEntry script);
    void DeleteScript(Guid shelfId, Guid scriptId);
    string ScriptFolder(Guid shelfId, Guid scriptId);
}

public sealed class FileScriptDataStore : IScriptDataStore
{
    public string DataRoot { get; }

    public FileScriptDataStore(string dataRoot)
    {
        DataRoot = dataRoot;
        Directory.CreateDirectory(DataRoot);
    }

    private string IndexPath => Path.Combine(DataRoot, "index.json");

    public DataIndex LoadIndex()
    {
        if (!File.Exists(IndexPath))
            return new DataIndex();
        try
        {
            return JsonSerializer.Deserialize<DataIndex>(File.ReadAllText(IndexPath), JsonOptions.Instance)
                   ?? new DataIndex();
        }
        catch
        {
            return new DataIndex();
        }
    }

    public void SaveIndex(DataIndex index)
    {
        Directory.CreateDirectory(DataRoot);
        File.WriteAllText(IndexPath, JsonSerializer.Serialize(index, JsonOptions.Instance));
    }

    public string ShelfDirectory(Guid shelfId) =>
        Path.Combine(DataRoot, "shelves", shelfId.ToString("N"));

    public string ScriptFolder(Guid shelfId, Guid scriptId) =>
        Path.Combine(ShelfDirectory(shelfId), "scripts", scriptId.ToString("N"));

    public List<ScriptEntry> LoadScripts(Guid shelfId)
    {
        var scriptsDir = Path.Combine(ShelfDirectory(shelfId), "scripts");
        if (!Directory.Exists(scriptsDir))
            return new List<ScriptEntry>();
        var list = new List<ScriptEntry>();
        foreach (var dir in Directory.GetDirectories(scriptsDir))
        {
            var cfg = Path.Combine(dir, "script.json");
            if (!File.Exists(cfg))
                continue;
            try
            {
                var e = JsonSerializer.Deserialize<ScriptEntry>(File.ReadAllText(cfg), JsonOptions.Instance);
                if (e != null)
                    list.Add(e);
            }
            catch
            {
                /* skip broken */
            }
        }
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    public void SaveScript(Guid shelfId, ScriptEntry script)
    {
        var folder = ScriptFolder(shelfId, script.Id);
        Directory.CreateDirectory(folder);
        script.UpdatedUtc = DateTimeOffset.UtcNow;
        File.WriteAllText(Path.Combine(folder, "script.json"),
            JsonSerializer.Serialize(script, JsonOptions.Instance));
        var ext = script.Runtime switch
        {
            ScriptRuntimeKind.PowerShell => ".ps1",
            ScriptRuntimeKind.Cmd => ".cmd",
            _ => ".txt"
        };
        File.WriteAllText(Path.Combine(folder, "body" + ext), script.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void DeleteScript(Guid shelfId, Guid scriptId)
    {
        var folder = ScriptFolder(shelfId, scriptId);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }
}
