using System.Collections;
using UnityEngine;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance;

    private ITTS tts;
    public bool IsSpeaking => tts != null && tts.IsSpeaking;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        tts = new AndroidTTS();
#else
        tts = new WindowsTTS();
#endif
        tts.Initialize();
    }

    public void Speak(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            tts?.Speak(message);
    }

    public void Stop() => tts?.Stop();

    private void OnDestroy() => tts?.Shutdown();
    private void OnApplicationQuit() => tts?.Shutdown();
}
