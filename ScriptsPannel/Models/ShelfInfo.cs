namespace ScriptsPannel.Models;

public sealed class ShelfInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; }
}
