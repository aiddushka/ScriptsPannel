namespace ScriptsPannel.Models;

public sealed class ScriptEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ScriptRuntimeKind Runtime { get; set; } = ScriptRuntimeKind.PowerShell;
    /// <summary>Used when Runtime is Custom — full path to interpreter.</summary>
    public string? CustomRunnerPath { get; set; }
    public string Body { get; set; } = "";
    public bool RunAsAdministrator { get; set; }
    public bool Autostart { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
