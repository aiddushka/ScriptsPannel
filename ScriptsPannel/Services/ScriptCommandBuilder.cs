using System.Diagnostics;
using ScriptsPannel.Models;

namespace ScriptsPannel.Services;

public static class ScriptCommandBuilder
{
    public static string BuildRunCommandLine(IScriptDataStore store, Guid shelfId, ScriptEntry script)
    {
        var folder = store.ScriptFolder(shelfId, script.Id);
        var bodyPs1 = System.IO.Path.Combine(folder, "body.ps1");
        var bodyCmd = System.IO.Path.Combine(folder, "body.cmd");
        var bodyTxt = System.IO.Path.Combine(folder, "body.txt");

        return script.Runtime switch
        {
            ScriptRuntimeKind.PowerShell =>
                $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{bodyPs1}\"",
            ScriptRuntimeKind.Cmd =>
                $"cmd.exe /d /c \"{bodyCmd}\"",
            ScriptRuntimeKind.Custom =>
                BuildCustom(script.CustomRunnerPath, System.IO.File.Exists(bodyTxt) ? bodyTxt : bodyPs1),
            _ => $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{bodyPs1}\""
        };
    }

    private static string BuildCustom(string? runner, string bodyPath)
    {
        if (string.IsNullOrWhiteSpace(runner))
            return $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{bodyPath}\"";
        return $"\"{runner}\" \"{bodyPath}\"";
    }

    public static ProcessStartInfo BuildStartInfo(IScriptDataStore store, Guid shelfId, ScriptEntry script)
    {
        var folder = store.ScriptFolder(shelfId, script.Id);
        var psi = new ProcessStartInfo { UseShellExecute = true };
        if (script.RunAsAdministrator)
            psi.Verb = "runas";

        switch (script.Runtime)
        {
            case ScriptRuntimeKind.PowerShell:
                psi.FileName = "powershell.exe";
                psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{System.IO.Path.Combine(folder, "body.ps1")}\"";
                break;
            case ScriptRuntimeKind.Cmd:
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/d /c \"\"{System.IO.Path.Combine(folder, "body.cmd")}\"\"";
                break;
            case ScriptRuntimeKind.Custom:
                if (string.IsNullOrWhiteSpace(script.CustomRunnerPath))
                    goto case ScriptRuntimeKind.PowerShell;
                psi.FileName = script.CustomRunnerPath;
                psi.Arguments = $"\"{System.IO.Path.Combine(folder, System.IO.File.Exists(System.IO.Path.Combine(folder, "body.txt")) ? "body.txt" : "body.ps1")}\"";
                break;
        }
        return psi;
    }
}
