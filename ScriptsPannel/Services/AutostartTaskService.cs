using System.Diagnostics;

namespace ScriptsPannel.Services;

/// <summary>Registers logon scheduled tasks for scripts (отключение — в приложении или в Планировщике заданий).</summary>
public interface IAutostartTaskService
{
    bool TryRegister(string taskName, string trCommand, bool runHighest);
    bool TryUnregister(string taskName);
}

public sealed class WindowsAutostartTaskService : IAutostartTaskService
{
    public const string TaskRoot = "ScriptsPannel";

    public static string FullTaskName(string shelfIdN, string scriptIdN) =>
        $"{TaskRoot}\\{shelfIdN}\\{scriptIdN}";

    public bool TryRegister(string taskName, string trCommand, bool runHighest)
    {
        try
        {
            TryUnregister(taskName);
            var rl = runHighest ? "HIGHEST" : "LIMITED";
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("/Create");
            psi.ArgumentList.Add("/F");
            psi.ArgumentList.Add("/TN");
            psi.ArgumentList.Add(taskName);
            psi.ArgumentList.Add("/SC");
            psi.ArgumentList.Add("ONLOGON");
            psi.ArgumentList.Add("/RL");
            psi.ArgumentList.Add(rl);
            psi.ArgumentList.Add("/TR");
            psi.ArgumentList.Add(trCommand);
            using var p = Process.Start(psi);
            p?.WaitForExit(30_000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool TryUnregister(string taskName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("/Delete");
            psi.ArgumentList.Add("/F");
            psi.ArgumentList.Add("/TN");
            psi.ArgumentList.Add(taskName);
            using var p = Process.Start(psi);
            p?.WaitForExit(15_000);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
