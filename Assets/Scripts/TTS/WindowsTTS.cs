using System;
using System.Diagnostics;

public class WindowsTTS : ITTS
{
    private Process currentProcess;
    public bool IsSpeaking { get; private set; }

    public void Initialize() { }

    public void Speak(string message)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Stop();
        if (string.IsNullOrWhiteSpace(message)) return;
        IsSpeaking = true;

        string escaped = message.Replace("'", "''");
        string cmd = "Add-Type -AssemblyName System.Speech;" +
                     "$v=New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
                     "$v.Speak('" + escaped + "');";

        currentProcess = new Process();
        currentProcess.StartInfo.FileName = "powershell.exe";
        currentProcess.StartInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"";
        currentProcess.StartInfo.UseShellExecute = false;
        currentProcess.StartInfo.CreateNoWindow = true;
        currentProcess.EnableRaisingEvents = true;
        currentProcess.Exited += (s, e) => { IsSpeaking = false; currentProcess?.Dispose(); currentProcess = null; };
        currentProcess.Start();
#endif
    }

    public void Stop()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            if (currentProcess != null)
            {
                if (!currentProcess.HasExited) currentProcess.Kill();
                currentProcess.Dispose();
                currentProcess = null;
            }
        }
        catch { }
#endif
        IsSpeaking = false;
    }

    public void Shutdown() => Stop();
}
