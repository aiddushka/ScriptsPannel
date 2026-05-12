using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public static class ShelfArchiveService
{
    public static void ExportShelfToZip(IScriptDataStore store, Guid shelfId, string displayName, string zipFilePath)
    {
        var root = store.ShelfDirectory(shelfId);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(root);

        if (File.Exists(zipFilePath))
            File.Delete(zipFilePath);

        using var fs = File.Create(zipFilePath);
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            var manifest = new ShelfExportManifest
            {
                FormatVersion = 1,
                ShelfName = displayName,
                ExportedUtc = DateTimeOffset.UtcNow
            };
            var man = zip.CreateEntry("manifest.json");
            using (var sw = new StreamWriter(man.Open(), Encoding.UTF8))
                sw.Write(JsonSerializer.Serialize(manifest, JsonOptions.Instance));

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
                zip.CreateEntryFromFile(file, rel);
            }
        }
    }

    public static bool TryImportShelfFromZip(
        IScriptDataStore store,
        IAutostartTaskService autostart,
        string zipFilePath,
        out ShelfInfo? shelfInfo,
        out string? errorMessage)
    {
        shelfInfo = null;
        errorMessage = null;
        var temp = Path.Combine(Path.GetTempPath(), "ScriptsPannel_import_" + Guid.NewGuid().ToString("N"));
        var newShelfId = Guid.Empty;
        var shelfDirCreated = false;

        try
        {
            Directory.CreateDirectory(temp);
            ZipFile.ExtractToDirectory(zipFilePath, temp);

            var shelfName = Path.GetFileNameWithoutExtension(zipFilePath);
            var manifestPath = Path.Combine(temp, "manifest.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var m = JsonSerializer.Deserialize<ShelfExportManifest>(File.ReadAllText(manifestPath, Encoding.UTF8),
                        JsonOptions.Instance);
                    if (m != null && !string.IsNullOrWhiteSpace(m.ShelfName))
                        shelfName = m.ShelfName.Trim();
                }
                catch
                {
                    /* keep filename */
                }
            }

            var scriptsRoot = Path.Combine(temp, "scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                errorMessage = "invalid_archive";
                return false;
            }

            newShelfId = Guid.NewGuid();
            Directory.CreateDirectory(store.ShelfDirectory(newShelfId));
            shelfDirCreated = true;

            var entries = new List<ScriptEntry>();
            foreach (var dir in Directory.GetDirectories(scriptsRoot))
            {
                var cfg = Path.Combine(dir, "script.json");
                if (!File.Exists(cfg))
                    continue;
                ScriptEntry? entry;
                try
                {
                    entry = JsonSerializer.Deserialize<ScriptEntry>(File.ReadAllText(cfg, Encoding.UTF8),
                        JsonOptions.Instance);
                }
                catch
                {
                    continue;
                }

                if (entry == null)
                    continue;

                entry.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(entry.Name))
                    entry.Name = "Script";

                var diskBody = ReadBodyFromScriptFolder(dir);
                if (!string.IsNullOrEmpty(diskBody))
                    entry.Body = diskBody;

                store.SaveScript(newShelfId, entry);
                entries.Add(entry);
            }

            if (entries.Count == 0)
            {
                errorMessage = "no_scripts_in_archive";
                try
                {
                    var emptyDir = store.ShelfDirectory(newShelfId);
                    if (Directory.Exists(emptyDir))
                        Directory.Delete(emptyDir, recursive: true);
                }
                catch
                {
                    /* ignore */
                }

                return false;
            }

            var info = new ShelfInfo
            {
                Id = newShelfId,
                Name = string.IsNullOrWhiteSpace(shelfName) ? "Imported" : shelfName,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var idx = store.LoadIndex();
            idx.Shelves.Add(info);
            store.SaveIndex(idx);

            foreach (var e in entries)
                SyncAutostartForScript(store, autostart, newShelfId, e);

            shelfInfo = info;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            if (shelfDirCreated && newShelfId != Guid.Empty)
            {
                try
                {
                    var d = store.ShelfDirectory(newShelfId);
                    if (Directory.Exists(d))
                        Directory.Delete(d, recursive: true);
                }
                catch
                {
                    /* ignore */
                }

                try
                {
                    var idx = store.LoadIndex();
                    if (idx.Shelves.RemoveAll(s => s.Id == newShelfId) > 0)
                        store.SaveIndex(idx);
                }
                catch
                {
                    /* ignore */
                }
            }

            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(temp))
                    Directory.Delete(temp, recursive: true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static string ReadBodyFromScriptFolder(string scriptDir)
    {
        foreach (var ext in new[] { ".ps1", ".cmd", ".txt" })
        {
            var p = Path.Combine(scriptDir, "body" + ext);
            if (File.Exists(p))
                return File.ReadAllText(p, new UTF8Encoding(false));
        }

        return "";
    }

    private static void SyncAutostartForScript(IScriptDataStore store, IAutostartTaskService autostart, Guid shelfId,
        ScriptEntry entry)
    {
        var taskName = WindowsAutostartTaskService.FullTaskName(shelfId.ToString("N"), entry.Id.ToString("N"));
        if (entry.Autostart)
        {
            var tr = ScriptCommandBuilder.BuildRunCommandLine(store, shelfId, entry);
            autostart.TryRegister(taskName, tr, entry.RunAsAdministrator);
        }
        else
        {
            autostart.TryUnregister(taskName);
        }
    }
}
