namespace ScriptsPannel.Models;

public sealed class ShelfExportManifest
{
    public int FormatVersion { get; set; } = 1;
    public string ShelfName { get; set; } = "";
    public DateTimeOffset ExportedUtc { get; set; }
}
