namespace ScriptsPannel.Models;

/// <summary>Root index.json under data root.</summary>
public sealed class DataIndex
{
    public List<ShelfInfo> Shelves { get; set; } = new();
}
