using UnityEngine;
using Process = System.Diagnostics.Process;

public class WindowsTTS : MonoBehaviour, ITTS
{
    private Process currentProcess;
    private bool speaking = false;

    public bool IsSpeaking => speaking;

    public void Initialize()
    {
        Debug.Log("[WindowsTTS] Initialized");
    }

    public void Speak(string message)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        Stop();

        if (string.IsNullOrWhiteSpace(message))
            return;

        speaking = true;

        string escaped = message.Replace("'", "''");

        string command =
            "Add-Type -AssemblyName System.Speech; " +
            "$speak = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
            "$speak.Rate = 0; " +
            "$speak.Speak('" + escaped + "');";

        currentProcess = new Process();

        currentProcess.StartInfo.FileName = "powershell.exe";
        currentProcess.StartInfo.Arguments =
            "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"";

        currentProcess.StartInfo.CreateNoWindow = true;
        currentProcess.StartInfo.UseShellExecute = false;

        currentProcess.EnableRaisingEvents = true;
        currentProcess.Exited += OnSpeechFinished;

        currentProcess.Start();

#else
        Debug.Log(message);
#endif
    }

    public void Stop()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

        try
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                currentProcess.Kill();
            }
        }
        catch
        {
        }

#endif

        speaking = false;
    }

    public void Shutdown()
    {
        Stop();
    }

    public void SetRate(float rate)
    {
        // Not used in PowerShell version.
    }

    public void SetPitch(float pitch)
    {
        // Windows PowerShell speech doesn't support pitch.
    }

    private void OnSpeechFinished(object sender, System.EventArgs e)
    {
        speaking = false;
    }
}