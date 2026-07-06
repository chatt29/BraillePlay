using UnityEngine;
using System.Collections;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    [Header("Settings")]
    [Range(0.5f, 2f)]
    public float speechRate = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    public bool dontDestroyOnLoad = false;

    public bool debugLogs = true;

    private ITTS platformTTS;

    private Coroutine speakCoroutine;

    private bool initialized = false;

    public bool IsInitialized => initialized;

    public bool IsSpeaking
    {
        get
        {
            if (platformTTS == null)
                return false;

            return platformTTS.IsSpeaking;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Initialize()
    {
        if (initialized)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR

        platformTTS = gameObject.AddComponent<AndroidTTS>();

#else

        platformTTS = gameObject.AddComponent<WindowsTTS>();

#endif

        platformTTS.Initialize();

        platformTTS.SetRate(speechRate);
        platformTTS.SetPitch(pitch);

        initialized = true;

        if (debugLogs)
            Debug.Log("[TTS] Initialized");
    }

    public void Speak(string message)
    {
        Speak(message, true);
    }

    public void Speak(string message, bool interrupt)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!initialized)
            Initialize();

        if (interrupt)
            StopSpeaking();

        if (speakCoroutine != null)
            StopCoroutine(speakCoroutine);

        speakCoroutine = StartCoroutine(SpeakRoutine(message));
    }

    IEnumerator SpeakRoutine(string message)
    {
        yield return null;

        if (debugLogs)
            Debug.Log("[TTS] " + message);

        platformTTS.Speak(message);
    }

    public void StopSpeaking()
    {
        if (!initialized)
            return;

        if (speakCoroutine != null)
        {
            StopCoroutine(speakCoroutine);
            speakCoroutine = null;
        }

        platformTTS.Stop();
    }

    public void SetSpeechRate(float rate)
    {
        speechRate = Mathf.Clamp(rate, 0.5f, 2f);

        if (platformTTS != null)
            platformTTS.SetRate(speechRate);
    }

    public void SetPitch(float value)
    {
        pitch = Mathf.Clamp(value, 0.5f, 2f);

        if (platformTTS != null)
            platformTTS.SetPitch(pitch);
    }

    private void OnDestroy()
    {
        if (platformTTS != null)
            platformTTS.Shutdown();
    }
}

